using FairyGUI;
using FairyGUI.Extensions;
using FairyGUI.Foundations.Collections;
using UnityEngine;

namespace Scripts
{
    public class HierarchyPanelUI : MonoBehaviour
    {
        /* 虚拟树视图要构建表示节点的数据对象，通过 VirtualTreeViewNode 盛放数据对象并表达层级关系
         * 使用绘制节点回调 VirtualTreeView.treeNodeRenderer 控制更新显示内容
         */

        UIPanel panel;

        class NodeInfo
        {
            public string text;
        }

        private void Awake()
        {
            UIObjectFactory.SetPackageItemExtension("ui://CustomFeatures/HierarchyTreeView", typeof(VirtualTreeView));

            panel = GetComponent<UIPanel>();
        }

        private void Start()
        {
            var ui = panel.ui;
            var virtualTree = ui.GetChild("fakeTree") as VirtualTreeView;
            virtualTree.treeNodeRenderer += RenderTreeNodeMethod;
            var rootNode = virtualTree.RootNode;
            for (int i = 0; i < 5; i++)
            {
                AppendNodeTo(rootNode, 0, 5, 5);
            }
            virtualTree.MarkTreeHasChanged();

            // 

            virtualTree.onClickItem.Add(e =>
            {
                var tree = (VirtualTreeView)e.sender;
                if (e.data is GObject obj)
                {
                    int index = tree.ItemToItemIndex(obj);
                    var node = tree.GetNodeAt(index);
                    bool prevSelected = node.Selected;
                    node.Selected = !node.Selected;
                    bool currSelected = node.Selected;
                    var item = (NodeInfo)node.data;
                    print($"click {item.text} {prevSelected} => {currSelected}");

                    tree.MarkTreeHasChanged();
                }
            });

            virtualTree.List.onKeyDown.Add(e =>
            {
                var sender = (GList)e.sender;
                var tree = (VirtualTreeView)sender.parent;
                var key = e.inputEvent.keyCode;

                using var selections = ListHandle<int>.New();
                tree.List.GetSelection(selections);
                if (selections.Count != 0)
                {
                    int firstSelection = selections[0];
                    var node = tree.GetNodeAt(firstSelection);
                    var item = (NodeInfo)node.data;
                    print($"keyCode: {key}, item: {item.text}");
                }
            });
        }

        void RenderTreeNodeMethod(VirtualTreeViewNode node, GComponent obj)
        {
            var data = node.data as NodeInfo;
            obj.text = data.text;
        }

        static void AppendNodeTo(VirtualTreeViewNode node, int deep, int maxDepth, int number)
        {
            if (deep > maxDepth) { return; }
            for (int i = 0; i < number; i++)
            {
                var cell = node.Append(new NodeInfo
                {
                    text = $"{deep}-{i}"
                });
                AppendNodeTo(cell, deep + 1, maxDepth, number);

                cell.Expanded = true;
            }
        }
    }
}