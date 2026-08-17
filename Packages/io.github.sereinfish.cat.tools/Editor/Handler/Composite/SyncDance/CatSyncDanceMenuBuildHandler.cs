#region LICENSE
// /*
//  * CatTools - A simple Unity plugin to assist in creating VRChat Avatars
//  * Copyright (C) 2025  一只大猫条
//  *
//  * This program is free software: you can redistribute it and/or modify
//  * it under the terms of the GNU General Public License as published by
//  * the Free Software Foundation, either version 3 of the License, or
//  * (at your option) any later version.
//  *
//  * This program is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  * GNU General Public License for more details.
//  *
//  * You should have received a copy of the GNU General Public License
//  * along with this program.  If not, see <https://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using io.github.sereinfish.cat.tools.Components;
using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    /// <summary>
    /// 在 Generating 阶段构建 CatSyncDance 的临时 ExpressionsMenu（不写入、不保存），
    /// 并按需替换引用 / 安装到 MA Menu Installer / 合并进 VRCAvatarDescriptor。
    /// </summary>
    public class CatSyncDanceMenuBuildHandler : ComponentHandler<CatSyncDance>
    {
        private const string CatSyncDanceMenuName = "CatSyncDance";
        private const string NextPageName = "下一页";
        private const int DancesPerSubMenu = 8;
        private const int ParentContentPerPage = 7;
        private const string MaMenuInstallerTypeName = "nadena.dev.modular_avatar.core.ModularAvatarMenuInstaller";
        private const string MaMenuToAppendFieldName = "menuToAppend";

        public override BuildPhase Phase => BuildPhase.Generating;

        public override void Execute(BuildContext context, CatSyncDance entity)
        {
            if (!entity.autoBuildMenu) return;

            if (entity.dances == null || entity.dances.Length == 0)
            {
                Debug.LogWarning("[CatSyncDance] 未配置任何舞蹈，跳过自动菜单构建。");
                return;
            }

            var builtMenu = BuildMenu(entity);

            // 4. 同 GameObject 的 MA Menu Installer（反射访问，未安装 MA 时回退自带安装）
            var maHandled = TryHandleMaMenuInstaller(entity, builtMenu);

            // 3. 选择框不为空时，替换 Avatar 中其他组件对指定菜单的引用
            if (entity.expressionsMenu != null)
            {
                ReplaceReferences(context, entity, builtMenu);
            }

            // 5. 自带安装：合并到 VRCAvatarDescriptor 的 Expressions Menu
            if (!maHandled)
            {
                BuiltinInstall(context, builtMenu);
            }
        }

        /// <summary>
        /// 构建临时 ExpressionsMenu：
        /// 根菜单只放一个 “CatSyncDance” 子菜单；固定项与舞蹈子菜单全部放在该子菜单内。
        /// 舞蹈子菜单每页 8 个舞蹈 Toggle；父菜单按 7 内容 + “下一页” 分页。
        /// </summary>
        private static VRCExpressionsMenu BuildMenu(CatSyncDance entity)
        {
            var resolved = entity.ResolveDanceLocalIndices();

            var catSyncDanceMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            catSyncDanceMenu.name = CatSyncDanceMenuName;

            // 固定项（仅第一页）
            var fixedItems = new List<VRCExpressionsMenu.Control>();
            if (!string.IsNullOrEmpty(entity.syncControllerParameterName))
                fixedItems.Add(NewToggle("同步开关", entity.syncControllerParameterName, 1f));
            if (!string.IsNullOrEmpty(entity.volumeParameter))
                fixedItems.Add(NewRadial("音量", entity.volumeParameter));
            if (!string.IsNullOrEmpty(entity.speedParameter))
                fixedItems.Add(NewRadial("速度", entity.speedParameter));
            fixedItems.Add(NewToggle("停止跳舞", entity.controllerParameterName, 0f));

            // 舞蹈子菜单链接
            var danceLinks = BuildDanceSubMenuLinks(entity, resolved);

            FillPaginatedMenu(catSyncDanceMenu, fixedItems, danceLinks);

            var builtMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            builtMenu.name = "CatSyncDance_Root";
            builtMenu.controls = new List<VRCExpressionsMenu.Control>
            {
                NewSubMenuControl(CatSyncDanceMenuName, catSyncDanceMenu)
            };

            return builtMenu;
        }

        /// <summary>
        /// 构建舞蹈子菜单链接：无分类的放入 “Dance {index}”，有分类的放入 “{分类}{index}”，
        /// 每个子菜单最多 8 个舞蹈 Toggle，满 8 个开新的兄弟子菜单。
        /// </summary>
        private static List<VRCExpressionsMenu.Control> BuildDanceSubMenuLinks(CatSyncDance entity, int[] resolved)
        {
            var defaultGroup = new List<DanceRef>();
            var categoryGroups = new Dictionary<string, List<DanceRef>>();
            var categoryOrder = new List<string>();

            // 类别顺序：danceCategories 主列表优先
            if (entity.danceCategories != null)
            {
                foreach (var category in entity.danceCategories)
                {
                    if (string.IsNullOrWhiteSpace(category) || categoryGroups.ContainsKey(category)) continue;
                    categoryGroups[category] = new List<DanceRef>();
                    categoryOrder.Add(category);
                }
            }

            for (var i = 0; i < entity.dances.Length; i++)
            {
                var dance = entity.dances[i];
                if (dance == null) continue;

                var categories = GetEffectiveCategories(dance);
                if (categories.Count == 0)
                {
                    defaultGroup.Add(new DanceRef(dance.danceName, resolved[i]));
                    continue;
                }

                foreach (var category in categories)
                {
                    if (!categoryGroups.TryGetValue(category, out var list))
                    {
                        list = new List<DanceRef>();
                        categoryGroups[category] = list;
                        categoryOrder.Add(category);
                    }
                    list.Add(new DanceRef(dance.danceName, resolved[i]));
                }
            }

            var links = new List<VRCExpressionsMenu.Control>();

            // 默认分类（无分类）
            AddGroupSubMenus(links, defaultGroup, entity.controllerParameterName, i => $"Dance {i}");

            // 各分类
            foreach (var category in categoryOrder)
            {
                AddGroupSubMenus(links, categoryGroups[category], entity.controllerParameterName, i => $"{category}{i}");
            }

            return links;
        }

        private static List<string> GetEffectiveCategories(CatSyncDanceEntry dance)
        {
            var result = new List<string>();
            if (dance?.categories == null) return result;
            foreach (var category in dance.categories)
            {
                if (!string.IsNullOrWhiteSpace(category)) result.Add(category);
            }
            return result;
        }

        private static void AddGroupSubMenus(
            List<VRCExpressionsMenu.Control> links,
            List<DanceRef> dances,
            string parameterName,
            Func<int, string> nameFor)
        {
            var subIndex = 0;
            for (var start = 0; start < dances.Count; start += DancesPerSubMenu)
            {
                var subMenuName = nameFor(subIndex);
                var subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                subMenu.name = subMenuName;
                subMenu.controls = new List<VRCExpressionsMenu.Control>();

                var end = Math.Min(start + DancesPerSubMenu, dances.Count);
                for (var k = start; k < end; k++)
                {
                    var dance = dances[k];
                    subMenu.controls.Add(NewToggle(dance.Name, parameterName, dance.LocalIndex));
                }

                links.Add(NewSubMenuControl(subMenuName, subMenu));
                subIndex++;
            }
        }

        /// <summary>
        /// 分页填充：第一页 = 固定项 + 舞蹈子菜单链接，最多 7 个内容项 + “下一页”；
        /// 后续页 = 最多 7 个内容项 + “下一页”。
        /// </summary>
        private static void FillPaginatedMenu(
            VRCExpressionsMenu firstPageMenu,
            List<VRCExpressionsMenu.Control> fixedItems,
            List<VRCExpressionsMenu.Control> danceLinks)
        {
            var linkIndex = 0;
            var pageControls = new List<VRCExpressionsMenu.Control>(fixedItems);

            while (pageControls.Count < ParentContentPerPage && linkIndex < danceLinks.Count)
            {
                pageControls.Add(danceLinks[linkIndex]);
                linkIndex++;
            }

            VRCExpressionsMenu nextPage = null;
            if (linkIndex < danceLinks.Count)
            {
                nextPage = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                nextPage.name = "CatSyncDance_Page2";
                pageControls.Add(NewSubMenuControl(NextPageName, nextPage));
            }
            firstPageMenu.controls = pageControls;

            while (nextPage != null)
            {
                var controls = new List<VRCExpressionsMenu.Control>();
                while (controls.Count < ParentContentPerPage && linkIndex < danceLinks.Count)
                {
                    controls.Add(danceLinks[linkIndex]);
                    linkIndex++;
                }

                VRCExpressionsMenu next = null;
                if (linkIndex < danceLinks.Count)
                {
                    next = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                    next.name = "CatSyncDance_PageNext";
                    controls.Add(NewSubMenuControl(NextPageName, next));
                }
                nextPage.controls = controls;
                nextPage = next;
            }
        }

        /// <summary>
        /// 需求 4：检查同 GameObject 上的 MA Menu Installer（反射，避免硬依赖 MA）。
        /// “要安装的菜单”（menuToAppend）为空或等于指定菜单时，把构建的菜单写入并返回 true；否则返回 false。
        /// </summary>
        private static bool TryHandleMaMenuInstaller(CatSyncDance entity, VRCExpressionsMenu builtMenu)
        {
            var maType = FindMaMenuInstallerType();
            if (maType == null) return false; // MA 未安装

            var installer = entity.GetComponent(maType);
            if (installer == null) return false; // 同 GameObject 没有 MA Menu Installer

            var menuToAppendField = maType.GetField(MaMenuToAppendFieldName, BindingFlags.Public | BindingFlags.Instance);
            if (menuToAppendField == null) return false;

            var current = menuToAppendField.GetValue(installer) as VRCExpressionsMenu;
            if (current == null || current == entity.expressionsMenu)
            {
                menuToAppendField.SetValue(installer, builtMenu);
                return true;
            }

            return false; // “要安装的菜单”是别的菜单 → 走自带安装
        }

        private static Type FindMaMenuInstallerType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(MaMenuInstallerTypeName, false);
                if (type != null) return type;
            }
            return null;
        }

        /// <summary>
        /// 需求 3：把 Avatar 内其他组件（排除 CatSyncDance 与 VRCAvatarDescriptor）中
        /// 值等于指定菜单的 VRCExpressionsMenu 引用替换为构建的临时菜单。
        /// </summary>
        private static void ReplaceReferences(BuildContext context, CatSyncDance entity, VRCExpressionsMenu builtMenu)
        {
            var target = entity.expressionsMenu;
            if (target == null) return;

            var components = context.AvatarRootTransform.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null) continue;
                if (component is CatSyncDance) continue;
                if (component is VRCAvatarDescriptor) continue;

                var serialized = new SerializedObject(component);
                var iterator = serialized.GetIterator();
                var changed = false;

                while (iterator.Next(true))
                {
                    if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (iterator.objectReferenceValue != target) continue;

                    iterator.objectReferenceValue = builtMenu;
                    changed = true;
                }

                if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// 需求 5：自带安装 —— 把构建菜单的 controls 合并进 VRCAvatarDescriptor 的 Expressions Menu。
        /// 非破坏：先克隆原菜单再合并；descriptor 的 Menu 为空则放弃。
        /// </summary>
        private static void BuiltinInstall(BuildContext context, VRCExpressionsMenu builtMenu)
        {
            var descriptor = context.VRChatAvatarDescriptor();
            if (descriptor == null)
            {
                Debug.LogWarning("[CatSyncDance] 未找到 VRCAvatarDescriptor，放弃自带菜单安装。");
                return;
            }

            var rootMenu = descriptor.expressionsMenu;
            if (rootMenu == null)
            {
                Debug.LogWarning("[CatSyncDance] VRCAvatarDescriptor 的 Expressions Menu 为空，放弃合并。");
                return;
            }

            var clone = (VRCExpressionsMenu)UnityEngine.Object.Instantiate(rootMenu);
            clone.name = rootMenu.name + "_CT_Menu";
            if (clone.controls == null)
                clone.controls = new List<VRCExpressionsMenu.Control>();
            foreach (var control in builtMenu.controls)
            {
                clone.controls.Add(control);
            }

            descriptor.expressionsMenu = clone;
        }

        private static VRCExpressionsMenu.Control NewToggle(string name, string parameter, float value)
        {
            return new VRCExpressionsMenu.Control
            {
                name = name,
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = parameter },
                value = value,
                labels = Array.Empty<VRCExpressionsMenu.Control.Label>(),
                subParameters = Array.Empty<VRCExpressionsMenu.Control.Parameter>()
            };
        }

        private static VRCExpressionsMenu.Control NewRadial(string name, string rotationParameter)
        {
            return new VRCExpressionsMenu.Control
            {
                name = name,
                type = VRCExpressionsMenu.Control.ControlType.RadialPuppet,
                subParameters = new[] { new VRCExpressionsMenu.Control.Parameter { name = rotationParameter } },
                labels = Array.Empty<VRCExpressionsMenu.Control.Label>()
            };
        }

        private static VRCExpressionsMenu.Control NewSubMenuControl(string name, VRCExpressionsMenu subMenu)
        {
            return new VRCExpressionsMenu.Control
            {
                name = name,
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = subMenu,
                labels = Array.Empty<VRCExpressionsMenu.Control.Label>(),
                subParameters = Array.Empty<VRCExpressionsMenu.Control.Parameter>()
            };
        }

        private readonly struct DanceRef
        {
            public readonly string Name;
            public readonly int LocalIndex;

            public DanceRef(string name, int localIndex)
            {
                Name = name;
                LocalIndex = localIndex;
            }
        }
    }
}
