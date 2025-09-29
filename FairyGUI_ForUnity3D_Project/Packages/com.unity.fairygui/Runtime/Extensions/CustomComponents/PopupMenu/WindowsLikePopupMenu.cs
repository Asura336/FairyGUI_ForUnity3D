#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using NextMenuData = FairyGUI.Extensions.WindowsLikePopupMenu;

#pragma warning disable IDE1006 // 命名样式
namespace FairyGUI.Extensions
{
    /// <summary>
    /// 原版的弹出菜单实现有问题，复制源码稍加改动。
    /// 用作弹出菜单的组件必须有一个名叫 list 的列表，单列竖排，自动调整列表项目大小，列表关联容器整宽，容器关联列表整高。
    /// 列表可以有垂直滚动条。
    /// 另见：<see cref="WindowsLikePopupMenuItem"/>
    /// </summary>
    public class WindowsLikePopupMenu : EventDispatcher
    {
        protected readonly GComponent _contentPane = null!;
        protected readonly GList _list = null!;

        protected readonly string? resourceURL;
        protected readonly string? labelURL;
        protected string? separatorURL;

        protected GObject? _expandingItem;
        protected WindowsLikePopupMenu? _parentMenu;
        readonly TimerCallback _showSubMenu = null!;
        readonly TimerCallback _closeSubMenu = null!;
        TimerCallback? _checkOnRollOut;

        EventListener? _onPopup;
        EventListener? _onClose;

        public int visibleItemCount = 10;
        public bool hideOnClickItem = true;
        public bool autoSize = true;
        /// <summary>
        /// 这个值在每次展开菜单时更新，同一次展开菜单时上层和下层控件的 season 一致。
        ///作用是在菜单使用池回收时阻止因为菜单持有下层菜单的引用置空过晚导致菜单实体被重用后被错误隐藏
        /// </summary>
        internal int season = 0;

        const string EVENT_TYPE = "PopupMenuItemClick";

        readonly List<GButton> items = new List<GButton>(32);
        int focusIndex = -1;

        readonly Vector2 defaultItemSize;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourceURL"></param>
        /// <param name="separatorURL"></param>
        /// <param name="labelURL"></param>
        public WindowsLikePopupMenu(string? resourceURL, string? separatorURL, string? labelURL)
        {
            this.resourceURL = resourceURL;
            this.labelURL = labelURL;
            this.separatorURL = separatorURL;

            if ((this.resourceURL ??= UIConfig.popupMenu) == null)
            {
                Debug.LogError("FairyGUI: UIConfig.popupMenu not defined");
                return;
            }

            _contentPane = UIPackage.CreateObjectFromURL(this.resourceURL).asCom;
            _contentPane.onAddedToStage.Add(__addedToStage);
            _contentPane.onRemovedFromStage.Add(__removeFromStage);
            _contentPane.focusable = false;

            _list = _contentPane.GetChild("list").asList;
            _list.RemoveChildrenToPool();

            _list.AddRelation(_contentPane, RelationType.Width);
            _list.RemoveRelation(_contentPane, RelationType.Height);
            _contentPane.AddRelation(_list, RelationType.Height);

            _list.onClickItem.Add(__clickItem);

            _showSubMenu = __showSubMenu;
            _closeSubMenu = CloseSubMenu;

            // focus
            _contentPane.focusable = true;
            _list.tabStopChildren = true;
            _list.onKeyDown.Add(List_onKeyDown);

            var listItem = UIPackage.GetItemByURL(_list.defaultItem);
            if (listItem == null)
            {
                Debug.LogWarning($"FairyGUI: List {resourceURL} has not default item url");
                defaultItemSize = new Vector2(24, 24);
            }
            else
            {
                defaultItemSize = new Vector2(listItem.width, listItem.height);
            }
        }

        public EventListener onPopup => _onPopup ??= new EventListener(this, "onPopup");

        public EventListener onClose => _onClose ??= new EventListener(this, "onClose");

        /// <summary>
        /// 传递元件引用给菜单，当菜单关闭时切换焦点到此元件
        /// </summary>
        public GObject? BaseFocus { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public GButton AddItem(string caption, EventCallback0 callback)
        {
            var item = CreateItem(caption, callback);
            _list.AddChild(item);
            items.Add(item);

            return item;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public GButton AddItem(string caption, EventCallback1 callback)
        {
            var item = CreateItem(caption, callback);
            _list.AddChild(item);
            items.Add(item);

            return item;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="caption"></param>
        ///// <param name="index"></param>
        ///// <param name="callback"></param>
        ///// <returns></returns>
        //public GButton AddItemAt(string caption, int index, EventCallback1? callback)
        //{
        //    var item = CreateItem(caption, callback);
        //    _list.AddChildAt(item, index);
        //    items.Insert(index, item);

        //    return item;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="caption"></param>
        ///// <param name="index"></param>
        ///// <param name="callback"></param>
        ///// <returns></returns>
        //public GButton AddItemAt(string caption, int index, EventCallback0? callback)
        //{
        //    var item = CreateItem(caption, callback);
        //    _list.AddChildAt(item, index);
        //    items.Insert(index, item);

        //    return item;
        //}

        GButton CreateItem(string caption, Delegate? callback)
        {
            var item = _list.GetFromPool(_list.defaultItem).asButton;
            item.title = caption;
            item.grayed = false;
            var c = item.GetController("checked");
            if (c != null)
            {
                c.selectedIndex = 0;
            }

            item.RemoveEventListeners(EVENT_TYPE);
            switch (callback)
            {
                case EventCallback0 callback0:
                    item.AddEventListener(EVENT_TYPE, callback0);
                    break;
                case EventCallback1 callback1:
                    item.AddEventListener(EVENT_TYPE, callback1);
                    break;
                default:
                    throw new NotSupportedException();
            }
            item.onRollOver.Add(__rollOver);
            item.onRollOut.Add(__rollOut);

            return item;
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddSeperator()
        {
            AddSeperator(-1);
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddSeperator(int index)
        {
            if ((separatorURL ??= UIConfig.popupMenu_seperator) == null)
            {
                Debug.LogError("FairyGUI: UIConfig.popupMenu_seperator not defined");
                return;
            }

            if (index == -1)
            {
                _list.AddItemFromPool(separatorURL);
            }
            else
            {
                var item = _list.GetFromPool(separatorURL);
                _list.AddChildAt(item, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="icon"></param>
        /// <param name="expanded"></param>
        public void AddLabel(string text, string? icon, bool expanded)
        {
            if (labelURL == null)
            {
                Debug.LogError($"FairyGUI: {nameof(WindowsLikePopupMenu)}::{nameof(labelURL)} is Nothing");
                return;
            }

            var item = _list.AddItemFromPool(labelURL).asCom;
            item.text = text;
            item.icon = icon;
            if (item.GetController("expanded") is Controller c_expanded)
            {
                c_expanded.selectedIndex = expanded ? 1 : 0;
            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="index"></param>
        ///// <returns></returns>
        //public string GetItemName(int index)
        //{
        //    var item = _list.GetChildAt(index).asButton;
        //    return item.name;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="caption"></param>
        //public void SetItemText(string name, string caption)
        //{
        //    var item = _list.GetChild(name).asButton;
        //    item.title = caption;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="visible"></param>
        //public void SetItemVisible(string name, bool visible)
        //{
        //    var item = _list.GetChild(name).asButton;
        //    if (item.visible != visible)
        //    {
        //        item.visible = visible;
        //        _list.SetBoundsChangedFlag();
        //    }
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="grayed"></param>
        //public void SetItemGrayed(string name, bool grayed)
        //{
        //    var item = _list.GetChild(name).asButton;
        //    item.grayed = grayed;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="checkable"></param>
        public void SetItemCheckable(string name, bool checkable)
        {
            var item = _list.GetChild(name).asButton;
            var c = item.GetController("checked");
            if (c != null)
            {
                if (checkable)
                {
                    if (c.selectedIndex == 0)
                    {
                        c.selectedIndex = 1;
                    }
                }
                else
                {
                    c.selectedIndex = 0;
                }
            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="check"></param>
        //public void SetItemChecked(string name, bool check)
        //{
        //    var item = _list.GetChild(name).asButton;
        //    var c = item.GetController("checked");
        //    if (c != null)
        //    {
        //        c.selectedIndex = check ? 2 : 1;
        //    }
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //public bool IsItemChecked(string name)
        //{
        //    var item = _list.GetChild(name).asButton;
        //    var c = item.GetController("checked");
        //    return c != null && c.selectedIndex == 2;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="name"></param>
        //public void RemoveItem(string name)
        //{
        //    var item = _list.GetChild(name).asCom;
        //    if (item != null)
        //    {
        //        item.RemoveEventListeners(EVENT_TYPE);
        //        if (item.data is NextMenuData _menu)
        //        {
        //            _menu.Dispose();
        //            item.data = null;
        //        }
        //        int index = _list.GetChildIndex(item);
        //        _list.RemoveChildToPoolAt(index);
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        public void ClearItems()
        {
            _list.RemoveChildrenToPool();
            items.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public int itemCount
        {
            get { return _list.numChildren; }
        }

        /// <summary>
        /// 
        /// </summary>
        public GComponent contentPane
        {
            get { return _contentPane; }
        }

        /// <summary>
        /// 
        /// </summary>
        public GList list
        {
            get { return _list; }
        }

        public bool isShowing { get; private set; } = false;

        public void Dispose()
        {
            int cnt = _list.numChildren;
            for (int i = 0; i < cnt; i++)
            {
                var obj = _list.GetChildAt(i);
                if (obj.data is NextMenuData menu) { menu.Dispose(); }
            }
            _contentPane.Dispose();
        }

        ///// <summary>
        ///// 
        ///// </summary>
        //public void Show()
        //{
        //    Show(null, PopupDirection.Auto);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="target"></param>
        //public void Show(GObject target)
        //{
        //    Show(target, PopupDirection.Auto, null);
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="dir"></param>
        public void Show(GObject? target, PopupDirection dir)
        {
            Show(target, dir, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="dir"></param>
        /// <param name="parentMenu"></param>
        public void Show(GObject? target, PopupDirection dir, WindowsLikePopupMenu? parentMenu)
        {
            var r = target != null ? target.root : GRoot.inst;
            r.ShowPopup(this.contentPane, (target is GRoot) ? null : target, dir);
            list.RequestFocus();
            _parentMenu = parentMenu;
        }

        public void Hide()
        {
            _list.ClearSelection();
            if (contentPane.parent != null)
            {
                ((GRoot)contentPane.parent).HidePopup(contentPane);
            }
        }

        public void HideAll()
        {
            Hide();
            _parentMenu?.HideAll();
        }


        void List_onKeyDown(EventContext context)
        {
            /* 这里的键盘导航比较特殊
             * 因为弹出菜单全部是单选按钮，导航选择的语义不能是通常的选择按钮，那样会导致菜单指令在导航时直接执行
             * （GList 预置的键盘导航派发的是 onClickItem 事件）
             * 用高亮行为替代，但是做法很别扭
             */
            bool scrollToView = false;
            switch (context.inputEvent.keyCode)
            {
                case KeyCode.UpArrow:
                    focusIndex--;
                    scrollToView = true;
                    break;
                case KeyCode.DownArrow:
                    focusIndex++;
                    scrollToView = true;
                    break;
                case KeyCode.LeftArrow when _parentMenu != null:
                    _parentMenu.CloseSubMenu(_parentMenu);
                    return;
                case KeyCode.RightArrow when
                focusIndex >= 0 && focusIndex < items.Count
                && items[focusIndex].data is NextMenuData subMenu:
                    ShowSubMenu(items[focusIndex]);
                    return;
                case KeyCode.Backspace:
                    Hide();
                    Timers.inst.CallLater(_target =>
                    {
                        if (_target is WindowsLikePopupMenu _self
                        && _self._parentMenu is WindowsLikePopupMenu _parent)
                        {
                            _parent.BeginFocus(_parent.focusIndex);
                        }
                    }, this);
                    return;
                case KeyCode.Menu:
                case KeyCode.Escape:
                    HideAll();
                    return;
                case KeyCode.Return:
                    if (focusIndex >= 0 && focusIndex < items.Count)
                    {
                        _list.DispatchEvent("onClickItem", items[focusIndex]);
                    }
                    return;
            }
            if (focusIndex < 0)
            {
                focusIndex = items.Count - 1;
            }
            focusIndex %= items.Count;
            BeginFocus(focusIndex);
            if (scrollToView)
            {
                list.ScrollToView(focusIndex);
            }
            //Debug.Log($"focus => {focusIndex}");
        }

        void BeginFocus(int focusIndex = 0)
        {
            if (items.Count != 0)
            {
                int count = items.Count;
                list.ClearSelection();
                this.focusIndex = count == 0 ? -1 : focusIndex;
                if (this.focusIndex != -1)
                {
                    items[focusIndex].selected = true;
                }
                //this.focusIndex = count == 0 ? -1 : focusIndex;
                //for (int i = 0; i < count; i++)
                //{
                //    items[i].GetController("button").selectedPage = i == focusIndex ? GButton.OVER : GButton.UP;
                //}
            }
        }

        void ShowSubMenu(GObject item)
        {
            _expandingItem = item;
            var popup = (NextMenuData)item.data;
            // 新的下层菜单应该继承上层菜单的状态
            popup.visibleItemCount = visibleItemCount;
            popup.hideOnClickItem = hideOnClickItem;
            popup.autoSize = autoSize;
            popup.season = season;

            if (item is GButton _b)
            {
                _b.selected = true;
            }
            popup.Show(item, PopupDirection.Auto, this);

            /* 默认向右弹出，如果右侧被阻挡改为向左
             * 默认向下展开，如果下方被阻挡改为向上
             */
            //Debug.Log($"show sub -> {item.text}");
            //Debug.Log($"root of item is groot? {item.root == GRoot.inst}");

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

        void CloseSubMenu(object? param)
        {
            if (contentPane.isDisposed) { return; }

            if (_expandingItem == null) { return; }

            if (_expandingItem is GButton _itemButton)
            {
                _itemButton.selected = false;
            }
            if (_expandingItem.data is NextMenuData popup)
            {
                _expandingItem = null;
                // 在一些场合新展开的菜单会在对象池重用这个 popup，用 season 判断引用有效
                if (popup.season == season)
                {
                    popup.Hide();
                }
            }
        }

        private void __clickItem(EventContext context)
        {
            var item = ((GObject)context.data).asButton;
            if (item == null) { return; }

            if (item.grayed)
            {
                _list.selectedIndex = -1;
                return;
            }

            var c = item.GetController("checked");
            if (c != null && c.selectedIndex != 0)
            {
                if (c.selectedIndex == 1)
                {
                    c.selectedIndex = 2;
                }
                else
                {
                    c.selectedIndex = 1;
                }
            }

            if (hideOnClickItem)
            {
                _parentMenu?.Hide();
                Hide();
            }

            item.DispatchEvent(EVENT_TYPE, item); //event data is for backward compatibility 
        }

        void __addedToStage()
        {
            isShowing = true;

            DispatchEvent("onPopup", null);

            if (autoSize)
            {
                _list.EnsureBoundsCorrect();
                int cnt = _list.numChildren;
                float maxDelta = float.MinValue;
                float maxFontSize = float.MinValue;
                float maxDelta1 = float.MinValue;
                float maxFontSize1 = float.MinValue;
                for (int i = 0; i < cnt; i++)
                {
                    var obj = _list.GetChildAt(i).asButton;
                    if (obj == null) { continue; }

                    var tf = obj.GetTextField();
                    if (tf != null)
                    {
                        float v = tf.textWidth - tf.initWidth;
                        if (v > maxDelta)
                        {
                            maxDelta = v;
                            maxFontSize = Mathf.Max(maxFontSize, tf.textFormat.size);
                        }
                    }

                    if (obj.GetChild("remark") is GObject remarkObj)
                    {
                        var remarkTf = remarkObj switch
                        {
                            GButton _b => _b.GetTextField(),
                            GTextField _t => _t,
                            GLabel _l => _l.GetTextField(),
                            GComponent _c => _c.GetChild("title") as GTextField,
                            _ => null,
                        };
                        if (remarkTf != null)
                        {
                            float v = remarkTf.textWidth - remarkTf.initWidth;
                            if (v > maxDelta1)
                            {
                                maxDelta1 = v;
                                maxFontSize1 = Mathf.Max(maxFontSize1, remarkTf.textFormat.size);
                            }
                        }
                    }
                }

                // HACK: 如果列表需要横向扩张，多增加两个字符的宽度
                if (maxDelta > 0) { maxDelta += maxFontSize * 2; }
                if (maxDelta1 > 0) { maxDelta1 += maxFontSize * 2; }

                float itemDelta = Mathf.Max(0, maxDelta) + Mathf.Max(0, maxDelta1);

                contentPane.width = Mathf.Max(contentPane.initWidth,
                    contentPane.initWidth + itemDelta);
            }
            else
            {
                contentPane.width = contentPane.initWidth;
            }

            _list.selectedIndex = -1;
            //_list.ResizeToFit(visibleItemCount > 0 ? visibleItemCount : int.MaxValue, minSize: 10);
            CustomResizeToFit(visibleItemCount > 0 ? visibleItemCount : int.MaxValue, minSize: 10);

            // begin focus
            focusIndex = _parentMenu is null ? -1 : 0;
            BeginFocus(focusIndex);
        }
        void CustomResizeToFit(int itemCount, int minSize)
        {
            _list.EnsureBoundsCorrect();

            itemCount = Math.Min(itemCount, _list.numItems);
            Assert.IsFalse(_list.isVirtual);

            var _layout = _list.layout;
            if (itemCount == 0)
            {
                switch (_layout)
                {
                    case ListLayoutType.SingleColumn:
                    case ListLayoutType.FlowHorizontal:
                        _list.viewHeight = minSize;
                        break;
                    default:
                        _list.viewWidth = minSize;
                        break;
                }
            }
            else
            {
                int lastActiveIndex = itemCount - 1;
                while (lastActiveIndex >= 0)
                {
                    // last active obj
                    var obj = _list.GetChildAt(lastActiveIndex);
                    if (!_list.foldInvisibleItems || obj.visible) { break; }
                    lastActiveIndex--;
                }
                if (lastActiveIndex < 0)
                {
                    switch (_layout)
                    {
                        case ListLayoutType.SingleColumn:
                        case ListLayoutType.FlowHorizontal:
                            _list.viewHeight = minSize;
                            break;
                        default:
                            _list.viewWidth = minSize;
                            break;
                    }
                }
                else
                {
                    // 逐个计算宽度或者高度
                    // 列表里如果分隔符较多，GList 自带的自动调整大小可能让菜单提前出现滚动条
                    int numItem = _list.numItems;
                    switch (_layout)
                    {
                        case ListLayoutType.SingleColumn:
                        case ListLayoutType.FlowHorizontal:
                        // y side
                        {
                            float defItemHeight = defaultItemSize.y;
                            float targetHeight = defItemHeight * itemCount;
                            float y = targetHeight;
                            for (int ci = 0; ci < numItem; ci++)
                            {
                                var itemObj = _list.GetChildAt(ci);
                                y = itemObj.y + itemObj.height;
                                if (y >= targetHeight) { break; }
                            }
                            _list.viewHeight = y;
                        }
                        break;
                        default:
                        // x side
                        {
                            float defItemWidth = defaultItemSize.x;
                            float targetWidth = defItemWidth * itemCount;
                            float x = targetWidth;
                            for (int ci = 0; ci < numItem; ci++)
                            {
                                var itemObj = _list.GetChildAt(ci);
                                x = itemObj.x + itemObj.width;
                                if (x >= targetWidth) { break; }
                            }
                            _list.viewWidth = x;
                        }
                        break;
                    }
                }
            }
        }

        void __removeFromStage()
        {
            _parentMenu = null;

            if (_expandingItem != null)
            {
                Timers.inst.Add(0, 1, _closeSubMenu);
            }

            DispatchEvent("onClose", null);

            isShowing = false;

            if (BaseFocus != null)
            {
                BaseFocus.RequestFocus();
                BaseFocus = null;
            }
        }

        void __rollOver(EventContext context)
        {
            var item = (GObject)context.sender;
            if (item is WindowsLikePopupMenuItem menuItem)
            {
                menuItem.InvokeNextMenuFactory();
            }
            if ((item.data is NextMenuData) || _expandingItem != null)
            {
                Timers.inst.CallLater(_showSubMenu, item);

                // [2022-03-23] 使用异步的判断，检查鼠标在子菜单上
                if (_checkOnRollOut != null)
                {
                    Timers.inst.Remove(_checkOnRollOut);
                }
            }

            focusIndex = items.IndexOf(item.asButton);
            BeginFocus(focusIndex == -1 ? 0 : focusIndex);
        }

        void __showSubMenu(object param)
        {
            if (contentPane.isDisposed) { return; }

            var item = (GObject)param;
            var r = contentPane.root;
            if (r == null) { return; }

            if (_expandingItem != null)
            {
                if (_expandingItem == item) { return; }

                CloseSubMenu(null);
            }

            if (item.data is NextMenuData)
            {
                ShowSubMenu(item);
            }
        }

        void __rollOut(EventContext context)
        {
            if (_expandingItem == null) { return; }

            var item = (GObject)context.sender;
            Timers.inst.Remove(_showSubMenu);

            // [2022-03-23] 使用异步的判断，检查鼠标在子菜单上
            Timers.inst.Add(0.25f, 1, _checkOnRollOut ??= CheckOnRollOut, _expandingItem?.data);
        }

        void CheckOnRollOut(object expandingItem_data)
        {
            bool _closeSubMenu = true;
            if (contentPane.root != null)
            {
                if (expandingItem_data is NextMenuData popup)
                {
                    var cp = popup.contentPane;
                    var pt = cp.GlobalToLocal(Stage.inst.touchPosition);
                    if (pt.x >= 0 && pt.y >= 0 &&
                        pt.x < cp.width &&
                        pt.y < cp.height)
                    {
                        _closeSubMenu = false;
                    }
                }
            }
            if (_closeSubMenu)
            {
                CloseSubMenu(null);
            }
        }
    }
}
#pragma warning restore IDE1006 // 命名样式
