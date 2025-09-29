using System;
using System.Collections.Generic;
using FairyGUI.Utils;

namespace FairyGUI.Extensions
{
    /// <summary>
    /// FairyGUI 原生不支持树视图的虚拟列表版本，做一个。
    /// 制作过程同普通的树视图，列表部分装在另一个 <see cref="GComponent"/> 里，名字叫 list。
    /// 要使用到虚拟树视图的组件需要事先注册。
    /// 选中项目和折叠项目的行为由外部控制。
    /// 见：<see cref="treeNodeRenderer"/>,
    /// <see cref="beforeTreeNodeExpand"/>,
    /// <see cref="onClickItem"/>
    /// <see cref="MarkTreeHasChanged"/>,
    /// <see cref="VirtualTreeViewNode"/>
    /// </summary>
    public sealed class VirtualTreeView : GComponent
    {
        public delegate void RenderTreeNodeMethod(VirtualTreeViewNode node, GComponent obj);
        public delegate void BeforeTreeNodeExpandMethod(VirtualTreeViewNode node, bool expanded);


        GList list;
        EventListener _onClickItem;
        EventListener _onRightClickItem;
        readonly List<VirtualTreeViewNode> nodeList = new List<VirtualTreeViewNode>();
        readonly VirtualTreeViewNode treeRoot;
        bool anyChange = false;

        /// <summary>
        /// 切换节点展开需要的点击次数，0（阻止切换）、1（单击切换） 或者 2（双击切换）
        /// </summary>
        public int clickToExpand = 2;
        /// <summary>
        /// 节点渲染回调
        /// </summary>
        public RenderTreeNodeMethod treeNodeRenderer;
        /// <summary>
        /// 节点展开，重计算列表前有机会添加节点
        /// </summary>
        public BeforeTreeNodeExpandMethod beforeTreeNodeExpand;


        public VirtualTreeView() : base()
        {
            treeRoot = new VirtualTreeViewNode
            {
                __tree = this,
                Expanded = true,
            };
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            list = GetChild("list").asList;
            list.itemRenderer = List_itemRenderer;
            list.SetVirtual();
            list.onClickItem.Add(List_onClickItem);
        }

        public override void Dispose()
        {
            base.Dispose();
            treeRoot?.Release();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (anyChange)
            {
                CalculateTreeNodeList();
                anyChange = false;
            }
        }


        public VirtualTreeViewNode GetNodeAt(int index) => nodeList[index];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryGetItem(VirtualTreeViewNode node, out GObject item)
        {
            item = null;

            int index = node.ItemIndex;
            if (index < 0) { return false; }

            int childIndex = list.ItemIndexToChildIndex(index);
            if (childIndex > -1 && childIndex < list.numChildren)
            {
                item = list.GetChildAt(childIndex);
            }
            return item != null;
        }

        /// <summary>
        /// 提交一次树发生改动的消息，下一个轮询时刷新显示内容
        /// </summary>
        public void MarkTreeHasChanged()
        {
            // 异步处理...
            anyChange = true;
            //CalculateTreeNodeList();
        }

        public int ItemToItemIndex(GObject listItem)
        {
            int _childIndex = list.GetChildIndex(listItem);
            if (_childIndex < 0) { throw new NotSupportedException("item is not child of list"); }
            return list.ChildIndexToItemIndex(_childIndex);
        }

        /// <summary>
        /// see <see cref="GList.EnableSelectionFocusEvents(bool)"/>
        /// </summary>
        /// <param name="enabled"></param>
        public void EnableArrowKeyNavigation(bool enabled) => list.EnableArrowKeyNavigation(enabled);

        /// <summary>
        /// see <see cref="GList.RefreshVirtualList()"/>
        /// </summary>
        public void RefreshVirtualList() => list.RefreshVirtualList();

        public GList List => list;
        /// <summary>
        /// 每层级缩进，像素。
        /// </summary>
        public int IndentWidth { get; set; } = 15;
        public VirtualTreeViewNode RootNode => treeRoot;

#pragma warning disable IDE1006 // 命名样式
        public int numItems => nodeList.Count;
        public ListSelectionMode selectionMode
        {
            get => list.selectionMode;
            set => list.selectionMode = value;
        }
        public EventListener onClickItem => _onClickItem ??= new EventListener(this, "onClickItem");
        public EventListener onRightClickItem => _onRightClickItem ??= new EventListener(this, "onRightClickItem");
#pragma warning restore IDE1006 // 命名样式

        /// <summary>
        /// 当前树内容与列表内容一致，没有在等待的改动
        /// </summary>
        public bool Identity => !anyChange;


        internal void CallNodeExpand(VirtualTreeViewNode node, bool value)
        {
            beforeTreeNodeExpand?.Invoke(node, value);
            MarkTreeHasChanged();
        }


        void CalculateTreeNodeList()
        {
            int count = nodeList.Count;
            for (int i = 0; i < count; i++)
            {
                nodeList[i].ItemIndex = -1;
            }
            nodeList.Clear();

            AppendNodeDataWith(nodeList, treeRoot);
            count = nodeList.Count;
            for (int i = 0; i < count; i++)
            {
                nodeList[i].ItemIndex = i;
            }

            if (list != null)
            {
                int scrollIndex = (list.numItems != 0) ? list.ChildIndexToItemIndex(0) : 0;
                list.numItems = nodeList.Count;

                list.ClearSelection();
                count = nodeList.Count;
                for (int i = 0; i < count; i++)
                {
                    if (nodeList[i].Selected)
                    {
                        list.AddSelection(i, false);
                    }
                }
                list.ScrollToView(scrollIndex, false, true);
            }
        }
        static void AppendNodeDataWith(List<VirtualTreeViewNode> data, VirtualTreeViewNode root)
        {
            if (!root.Expanded) { return; }

            int count = root.Count;
            // 前序遍历
            for (int i = 0; i < count; i++)
            {
                data.Add(root[i]);
                AppendNodeDataWith(data, root[i]);
            }
        }

        EventCallback1 item_onExpandChanged;
        void List_itemRenderer(int index, GObject obj)
        {
            // tree => data => renderer
            var node = nodeList[index];
            var item = obj.asCom;

            int depth = node.Depth;
            if (GetIndent(item) is GObject indent)
            {
                indent.width = IndentWidth * (depth - 1);
            }

            bool expanded = node.Expanded;
            if (GetExpanded(item) is Controller ce)
            {
                ce.onChanged.Clear();
                ce.selectedIndex = expanded ? 1 : 0;
                ce.onChanged.Add(item_onExpandChanged ??= Item_onExpandChanged);
            }

            bool isLeaf = node.IsLeaf;
            if (GetLeaf(item) is Controller cl)
            {
                cl.selectedIndex = isLeaf ? 1 : 0;
            }

            treeNodeRenderer?.Invoke(node, item);
        }

        void List_onClickItem(EventContext e)
        {
            if (clickToExpand != 0 && e.data is GObject gObj)
            {
                //...
                int itemIndex = list.ChildIndexToItemIndex(list.GetChildIndex(gObj));
                var node = nodeList[itemIndex];
                if (clickToExpand != 2 || e.inputEvent.isDoubleClick)
                {
                    node.Expanded = !node.Expanded;
                }
            }

            DispatchEvent(e.type, e.data);
        }

        //static Func<Controller, GComponent> _getParent;
        void Item_onExpandChanged(EventContext e)
        {
            var c = (Controller)e.sender;
            //var item = (_getParent ??= GetParentFunc())(c);
            var item = c.parent;
            int itemIndex = ItemToItemIndex(item);
            var node = GetNodeAt(itemIndex);
            node.Expanded = c.selectedIndex != 0;
        }

        static Controller GetExpanded(GComponent cell) => cell.GetController("expanded");
        static Controller GetLeaf(GComponent cell) => cell.GetController("leaf");
        static GObject GetIndent(GComponent cell) => cell.GetChild("indent");
    }

    /// <summary>
    /// <see cref="VirtualTreeView"/> 中使用的节点
    /// </summary>
    public sealed class VirtualTreeViewNode
    {
        bool __expanded = false;
        bool __selected = false;

        internal VirtualTreeView __tree;

        /// <summary>
        /// 0 to n, root is 0
        /// </summary>
        int depth = 0;

        VirtualTreeViewNode parent;

        List<VirtualTreeViewNode> _children;

        /// <summary>
        /// 对应的数据实体
        /// </summary>
        public object data;

        public VirtualTreeViewNode()
        {

        }

        /// <summary>
        /// 增加节点，传染树的引用，层级+1
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public VirtualTreeViewNode Append(object data)
        {
            var node = new VirtualTreeViewNode
            {
                data = data,
                depth = depth + 1,
                parent = this,
                __tree = __tree
            };
            Children.Add(node);
            __tree?.MarkTreeHasChanged();
            return node;
        }

        /// <summary>
        /// 插入节点，传染树的引用，层级+1
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public VirtualTreeViewNode Insert(object data, int index)
        {
            var node = new VirtualTreeViewNode
            {
                data = data,
                depth = depth + 1,
                parent = this,
                __tree = __tree
            };
            Children.Insert(index, node);
            __tree?.MarkTreeHasChanged();
            return node;
        }

        /// <summary>
        /// 移除子级节点，也释放被移除节点
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            if (IsLeaf) { return; }
            _children[index].Release();
            _children.RemoveAt(index);
            __tree?.MarkTreeHasChanged();
        }

        public void Clear()
        {
            if (!IsLeaf)
            {
                for (int i = _children.Count - 1; i >= 0; i--)
                {
                    _children[i].Release();
                    _children.RemoveAt(i);
                }
            }
            __tree?.MarkTreeHasChanged();
        }

        public void Release()
        {
            if (_children != null)
            {
                for (int i = _children.Count - 1; i >= 0; i--)
                {
                    _children[i].Release();
                }
                _children.Clear();
            }
            parent = null;
        }

        public void ClearSelection()
        {
            __selected = false;
            if (_children != null)
            {
                for (int i = _children.Count - 1; i >= 0; i--)
                {
                    _children[i].ClearSelection();
                }
            }
            __tree?.MarkTreeHasChanged();
        }

        public bool Expanded
        {
            get => __expanded;
            set
            {
                if (__expanded != value)
                {
                    __expanded = value;
                    //if (__expanded)
                    //{
                    //    // 子节点展开，上层必然展开
                    //    for (var c = parent; c != null; c = c.parent)
                    //    {
                    //        c.__expanded = true;
                    //    }
                    //}
                    __tree?.CallNodeExpand(this, value);
                }
            }
        }

        public bool Selected
        {
            get => __selected;
            set
            {
                if (__selected != value)
                {
                    __selected = value;
                    __tree?.MarkTreeHasChanged();
                }
            }
        }

        public List<VirtualTreeViewNode> Children => _children ??= new List<VirtualTreeViewNode>();
        public bool IsLeaf => _children == null || _children.Count == 0;

        public int Count => _children == null ? 0 : _children.Count;
        public VirtualTreeViewNode this[int index]
        {
            get => _children?[index];
            set => _children[index] = value;
        }

        public int Depth => depth;

        public int ItemIndex { get; internal set; } = -1;

        public VirtualTreeViewNode Parent => parent;
        public VirtualTreeView Tree => __tree;
    }
}