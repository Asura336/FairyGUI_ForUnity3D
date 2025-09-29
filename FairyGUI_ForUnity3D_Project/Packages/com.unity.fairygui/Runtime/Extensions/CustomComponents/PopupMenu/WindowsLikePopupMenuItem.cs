#nullable enable
using System;
using FairyGUI.Utils;

namespace FairyGUI.Extensions
{
    public delegate void WindowsLikePopupMenuItemCallback(WindowsLikePopupMenuItem item, object? status);

    /// <summary>
    /// 一般菜单栏条目。
    /// 另见：<see cref="WindowsLikePopupMenu"/>
    /// <code>
    /// 组件：
    /// component (root) => title, icon, [remark], isChecked, hasNext, checked, nextMenu, [grayed]
    /// title => text, 上下居中|右延展-右
    /// icon => loader
    /// remark => text, 上下居中|右-右
    /// isChecked => loader
    /// hasNext => loader
    /// checked => controller: { 0:no, 1:yes, 2:itemChecked }
    /// nextMenu => controller: { 0, 1 }
    /// grayed => controller: { 0, 1 }
    /// </code>
    /// </summary>
    public class WindowsLikePopupMenuItem : GButton
    {
        Controller? _nextMenu = null;
        Controller? _checked = null;
        GObject? _remarkObj = null;

        #region closure
        internal Func<WindowsLikePopupMenu>? nextMenuFactory;
        internal WindowsLikePopupMenu? menuRef;
        internal WindowsLikePopupMenuItemCallback? method;
        internal object? methodStatus;
        internal void InvokeMethod() => method?.Invoke(this, methodStatus);
        // 和 弹出菜单 的实现兼容，弹出菜单项的下级菜单引用存放在 data 内
        internal WindowsLikePopupMenu? InvokeNextMenuFactory()
        {
            if (this.data is WindowsLikePopupMenu nextMenu) { return nextMenu; }

            var menu = nextMenuFactory?.Invoke();
            this.data = menu;
            return menu;
        }
        #endregion

        public bool HasNextMenu
        {
            get => _nextMenu != null && _nextMenu.selectedIndex != 0;
            set
            {
                if (_nextMenu != null)
                {
                    _nextMenu.selectedIndex = value ? 1 : 0;
                }
            }
        }

        public string? Remark
        {
            get => _remarkObj?.text ?? string.Empty;
            set
            {
                value ??= string.Empty;
                if (_remarkObj != null && _remarkObj.text != value)
                {
                    _remarkObj.text = value;
                }
            }
        }

        public bool ItemChecked
        {
            get => _checked != null && _checked.selectedIndex == 2;
            set
            {
                if (_checked != null)
                {
                    _checked.selectedIndex = value ? 2 : 1;
                }
            }
        }

        public GObject? IconObject => _iconObject;

        public GObject? TitleObject => _titleObject;


        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            _checked = GetController("checked");
            _nextMenu = GetController("nextMenu");

            _remarkObj = GetChild("remark");

            onRemovedFromStage.Add(Self_onRemovedFromStage);
        }

        public override void Dispose()
        {
            base.Dispose();
            nextMenuFactory = null;
            menuRef = null;
        }

        public void SetItemCheckable(bool checkable)
        {
            if (_checked != null)
            {
                _checked.selectedIndex = checkable ? 1 : 0;
            }
        }

        void Self_onRemovedFromStage()
        {
            // 对菜单项来说，弹出子菜单并收回后子菜单实例不会立即失效，所以不置空这些初始化选项
            //grayed = false;
            //SetItemCheckable(false);
            //HasNextMenu = false;
            //data = null;
            //icon = null;
            //Remark = string.Empty;
        }
    }
}
