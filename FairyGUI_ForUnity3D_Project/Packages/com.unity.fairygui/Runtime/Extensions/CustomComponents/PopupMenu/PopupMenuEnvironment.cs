#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CommonPopupMenuItem = FairyGUI.Extensions.WindowsLikePopupMenuItem;
using PopupMenuClass = FairyGUI.Extensions.WindowsLikePopupMenu;

namespace FairyGUI.Extensions
{
    /// <summary>
    ///  
    /// </summary>
    /// <param name="env"></param>
    /// <param name="menuObj"></param>
    public delegate void MenuDefinition(PopupMenuEnvironment env, PopupMenuClass menuObj);

    /// <summary>
    /// 包装菜单栏对象的池和资源路径，提供定义菜单栏的基础行为
    /// </summary>
    public class PopupMenuEnvironment
    {
        protected const int defaultCapacity = 8;
        protected readonly Queue<PopupMenuClass> pool;

        protected readonly string? resourceURL;
        protected readonly string? separatorURL;
        protected readonly string? labelURL;

        int envSeason = 0;

        public Action<Exception>? onException;

        public PopupMenuEnvironment() : this(null, null, defaultCapacity)
        {
        }

        public PopupMenuEnvironment(int capacity = defaultCapacity) : this(null, null, capacity)
        {
        }

        public PopupMenuEnvironment(string? resourceURL, int capacity = defaultCapacity)
            : this(resourceURL, null, capacity)
        {
        }

        public PopupMenuEnvironment(string? resourceURL, string? separatorURL, int capacity = defaultCapacity)
        {
            pool = new Queue<PopupMenuClass>(capacity);
            this.resourceURL = resourceURL;
            this.separatorURL = separatorURL;
        }

        public PopupMenuEnvironment(string? resourceURL, string? separatorURL, string? labelURL, int capacity = defaultCapacity)
            : this(resourceURL, separatorURL, capacity)
        {
            this.labelURL = labelURL;
        }

        /// <summary>
        /// 释放所有已经收起的菜单对象
        /// </summary>
        public void Release()
        {
            while (pool.Count != 0)
            {
                pool.Dequeue().Dispose();
            }
        }

        protected virtual void PopupMenu_onClosed(EventContext e)
        {
            var sender = (PopupMenuClass)e.sender;
            if (pool.Contains(sender)) { return; }

            pool.Enqueue(sender);

            var list = sender.list;
            int count = list.numItems;
            for (int i = 0; i < count; i++)
            {
                var item = list.GetChildAt(i);
                SetIconColor(item.asCom, Color.white);

                // see AppendNextMenuItem
                if (item.data is PopupMenuClass nextMenu)
                {
                    nextMenu.onClose.Call();
                }
            }
        }

        public PopupMenuClass GetEmptyInstance() => GetMenuInstance();


        public void AppendNormalItem(PopupMenuClass menu, string title, string? icon, WindowsLikePopupMenuItemCallback? onClick = null, object? status = null, bool enabled = true, string? remark = null)
        {
            var item = (CommonPopupMenuItem)menu.AddItem(title, InvokeCallbackAndCloseMenu);
            InternalAppendNormalItem(menu, item, onClick, status, icon, null, enabled, remark);
        }

        public void AppendNormalItem(PopupMenuClass menu, string title, string? icon, in Color iconColor, WindowsLikePopupMenuItemCallback? onClick = null, object? status = null, bool enabled = true, string? remark = null)
        {
            var item = (CommonPopupMenuItem)menu.AddItem(title, InvokeCallbackAndCloseMenu);
            InternalAppendNormalItem(menu, item, onClick, status, icon, iconColor, enabled, remark);
        }

        static void InternalAppendNormalItem(PopupMenuClass menu,
             CommonPopupMenuItem item,
             WindowsLikePopupMenuItemCallback? method,
             object? status,
             string? icon,
             Color? iconColor,
             bool enabled,
             string? remark)
        {
            item.icon = icon;
            SetIconColor(item, iconColor ?? Color.white);
            item.grayed = !enabled;
            item.touchable = enabled;
            item.data = null;

            // 用字段替代闭包
            item.nextMenuFactory = null;
            item.menuRef = menu;
            item.method = method;
            item.methodStatus = status;

            item.SetItemCheckable(false);
            item.HasNextMenu = false;
            item.Remark = remark;
        }


        public void AppendCheckableItem(PopupMenuClass menu, string title, bool isChecked, WindowsLikePopupMenuItemCallback? onClick = null, object? status = null, bool enabled = true, string? remark = null)
        {
            var item = (CommonPopupMenuItem)menu.AddItem(title, InvokeCallbackAndCloseMenu);
            InternalAppendCheckableItem(menu, item, onClick, status, isChecked, enabled, remark);
        }

        void InternalAppendCheckableItem(PopupMenuClass menu,
            CommonPopupMenuItem item,
            WindowsLikePopupMenuItemCallback? method,
            object? status,
            bool isChecked,
            bool enabled,
            string? remark)
        {
            item.icon = null;
            item.grayed = !enabled;
            item.touchable = enabled;
            item.data = null;

            // 用字段替代闭包
            item.nextMenuFactory = null;
            item.menuRef = menu;
            item.method = method;
            item.methodStatus = status;

            item.ItemChecked = isChecked;
            item.HasNextMenu = false;
            item.Remark = remark;
        }


        public void AppendNextMenuItem(PopupMenuClass menu, string title, string? icon, Func<PopupMenuClass> onNextMenu, bool enabled = true, string? remark = null)
        {
            var item = (CommonPopupMenuItem)menu.AddItem(title, ShowNextMenuImmediate);
            InternalAppendNextMenuItem(menu, item, icon, null, enabled, onNextMenu, remark);
        }

        public void AppendNextMenuItem(PopupMenuClass menu, string title, string? icon, in Color iconColor, Func<PopupMenuClass> onNextMenu, bool enabled = true, string? remark = null)
        {
            var item = (CommonPopupMenuItem)menu.AddItem(title, ShowNextMenuImmediate);
            InternalAppendNextMenuItem(menu, item, icon, iconColor, enabled, onNextMenu, remark);
        }

        void InternalAppendNextMenuItem(PopupMenuClass menu,
            CommonPopupMenuItem item,
            string? icon,
            Color? iconColor,
            bool enabled,
            Func<PopupMenuClass> onNextMenu,
            string? remark)
        {
            item.icon = icon;
            SetIconColor(item, iconColor ?? Color.white);
            item.grayed = !enabled;
            item.touchable = enabled;
            item.data = null;

            // 用字段替代闭包
            item.nextMenuFactory = onNextMenu;
            item.menuRef = menu;
            item.method = null;
            item.methodStatus = null;

            item.SetItemCheckable(false);
            item.HasNextMenu = true;
            item.Remark = remark;
        }


        public void AppendSeparator(PopupMenuClass menu) => menu.AddSeperator();

        public void AppendLabel(PopupMenuClass menu, string title, string? icon, bool expanded) => menu.AddLabel(title, icon, expanded);

        /// <summary>
        /// 从委托定义菜单栏内容，缓存返回的委托，调用返回的委托展示菜单。
        /// 委托的第一个参数是 <see cref="this"/>，第二个参数是池中取出的菜单对象。
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Func<PopupMenuClass> BuildMenuFunction(params MenuDefinition[] context) => BuildMenuFunction((IEnumerable<MenuDefinition>)context);

        /// <summary>
        /// 从委托定义菜单栏内容，缓存返回的委托，调用返回的委托展示菜单。
        /// 委托的第一个参数是 <see cref="this"/>，第二个参数是池中取出的菜单对象。
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Func<PopupMenuClass> BuildMenuFunction(MenuDefinition item) => () =>
        {
            var instance = GetMenuInstance();
            item(this, instance);
            return instance;
        };

        /// <summary>
        /// 从委托定义菜单栏内容，缓存返回的委托，调用返回的委托展示菜单。
        /// 委托的第一个参数是 <see cref="this"/>，第二个参数是池中取出的菜单对象。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="item1"></param>
        /// <returns></returns>
        public Func<PopupMenuClass> BuildMenuFunction(MenuDefinition item, MenuDefinition item1) => () =>
        {
            var instance = GetMenuInstance();
            item(this, instance);
            item1(this, instance);
            return instance;
        };

        /// <summary>
        /// 从委托定义菜单栏内容，缓存返回的委托，调用返回的委托展示菜单。
        /// 委托的第一个参数是 <see cref="this"/>，第二个参数是池中取出的菜单对象。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <returns></returns>
        public Func<PopupMenuClass> BuildMenuFunction(MenuDefinition item, MenuDefinition item1, MenuDefinition item2) => () =>
        {
            var instance = GetMenuInstance();
            item(this, instance);
            item1(this, instance);
            item2(this, instance);
            return instance;
        };

        /// <summary>
        /// 从委托定义菜单栏内容，缓存返回的委托，调用返回的委托展示菜单。
        /// 委托的第一个参数是 <see cref="this"/>，第二个参数是池中取出的菜单对象。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <param name="item3"></param>
        /// <returns></returns>
        public Func<PopupMenuClass> BuildMenuFunction(MenuDefinition item, MenuDefinition item1, MenuDefinition item2, MenuDefinition item3) => () =>
        {
            var instance = GetMenuInstance();
            item(this, instance);
            item1(this, instance);
            item2(this, instance);
            item3(this, instance);
            return instance;
        };

        /// <summary>
        /// 从委托定义菜单栏内容，缓存返回的委托，调用返回的委托展示菜单。
        /// 委托的第一个参数是 <see cref="this"/>，第二个参数是池中取出的菜单对象。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <param name="item3"></param>
        /// <param name="item4"></param>
        /// <returns></returns>
        public Func<PopupMenuClass> BuildMenuFunction(MenuDefinition item, MenuDefinition item1, MenuDefinition item2, MenuDefinition item3, MenuDefinition item4) => () =>
        {
            var instance = GetMenuInstance();
            item(this, instance);
            item1(this, instance);
            item2(this, instance);
            item3(this, instance);
            item4(this, instance);
            return instance;
        };

        /// <summary>
        /// 从委托定义菜单栏内容，缓存返回的委托，调用返回的委托展示菜单。
        /// 委托的第一个参数是 <see cref="this"/>，第二个参数是池中取出的菜单对象。
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Func<PopupMenuClass> BuildMenuFunction(List<MenuDefinition> context) => () =>
        {
            var instance = GetMenuInstance();
            foreach (var c in context)
            {
                c(this, instance);
            }
            return instance;
        };

        /// <summary>
        /// 从委托定义菜单栏内容，缓存返回的委托，调用返回的委托展示菜单。
        /// 委托的第一个参数是 <see cref="this"/>，第二个参数是池中取出的菜单对象。
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Func<PopupMenuClass> BuildMenuFunction(IEnumerable<MenuDefinition> context) => () =>
        {
            var instance = GetMenuInstance();
            foreach (var c in context)
            {
                c(this, instance);
            }
            return instance;
        };

        protected PopupMenuClass GetMenuInstance()
        {
            PopupMenuClass cell;
            if (pool.Count == 0)
            {
                cell = new PopupMenuClass(resourceURL, separatorURL, labelURL) { hideOnClickItem = false };
                cell.onClose.Add(PopupMenu_onClosed);
            }
            else
            {
                cell = pool.Dequeue();
                cell.ClearItems();
            }

            unchecked
            {
                cell.season = ++envSeason;
            }

            return cell;
        }

        protected static void InvokeCallbackAndCloseMenu(EventContext e)
        {
            var sender = e.sender as CommonPopupMenuItem;
            sender?.InvokeMethod();
            sender?.menuRef?.contentPane.root.HidePopup();
        }

        protected static void ShowNextMenuImmediate(EventContext e)
        {
            var item = (CommonPopupMenuItem)e.sender;
            var menu = item.menuRef ?? throw new NullReferenceException("owner menu is Nothing");
            var contentPane = menu.contentPane;
            if (item.InvokeNextMenuFactory() is PopupMenuClass popup)
            {
                popup.season = menu.season;
                popup.Show(item, PopupDirection.Auto, menu);

                var _thisRoot = item.root;
                var popupSize = popup.contentPane.size;
                var _localDelta = new Vector2(item.x + item.width - 4, item.y);
                var destination = contentPane.LocalToRoot(_localDelta, _thisRoot);

                bool _rightOut = destination.x + popupSize.x > _thisRoot.width;
                bool _downOut = destination.y + popupSize.y > _thisRoot.height;
                if (_rightOut || _downOut)
                {
                    destination = contentPane.LocalToRoot(
                        new Vector2(_rightOut ? item.x - popupSize.x + 4 : _localDelta.x,
                        _downOut ? item.y + item.height - popupSize.y : _localDelta.y), _thisRoot);
                }
                popup.contentPane.position = destination;
            }
        }

        static void SetIconColor(GObject? item, in Color color)
        {
            var iconObject = item switch
            {
                CommonPopupMenuItem commonItem => commonItem.IconObject,
                GComponent gCom => gCom.GetChild("icon"),
                _ => null,
            };
            switch (iconObject)
            {
                case GLoader loader:
                    loader.color = color;
                    break;
                case GButton button:
                    button.color = color;
                    break;
                default: break;
            }
        }
    }
}