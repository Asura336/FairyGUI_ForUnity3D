using System;
using FairyGUI;
using UnityEngine;

public class TestVirtualList : MonoBehaviour
{
    [Range(0, 20)]
    public int targetNumber = 10;

    GComponent contentPane;
    GList list;
    GList list_hz;

    //bool list_scrollbarVisible;

    /* FGUI 的虚拟列表有一个已知问题：
     * 如果虚拟列表有滚动条，列表控件将始终为滚动条留出空位，无论它当前是否显示
     * 
     * 修改源代码之外的替代方案：
     * 为特定的虚拟列表形态，在 itemRenderer 阶段修改绘制的控件尺寸
     */

    void Start()
    {
        var uiPanel = GetComponent<UIPanel>();
        contentPane = uiPanel.ui;
        list = contentPane.GetChild("list").asList;
        list.itemRenderer = list_itemRenderer;
        list.SetVirtual();
        
        list_hz=contentPane.GetChild("list-hz").asList;
        list_hz.itemRenderer = list_hz_itemRenderer;
        list_hz.SetVirtual();

        SetNumber();
    }

    private void list_itemRenderer(int index, GObject item)
    {
        //float list_viewWidth = list.viewWidth;
        //float list_scrollbarWidth = list.scrollPane.vtScrollBar.width;

        //item.width = list_scrollbarVisible
        //    ? list_viewWidth
        //    : list_viewWidth + list_scrollbarWidth;

        item.text = index.ToString();
    }

    private void list_hz_itemRenderer(int index, GObject item)
    {
        item.text = index.ToString();
    }

    [ContextMenu("apply list number")]
    public void SetNumber()
    {
        //var itemSize = list.defaultItemSize;
        //float itemHeight = itemSize.y;
        //int columnGap = list.columnGap;
        //double contentHeight = 0;
        //for (int i = 0; i < targetNumber; i++)
        //{
        //    contentHeight += itemHeight;
        //    if (i != 0 && i != targetNumber - 1)
        //    {
        //        contentHeight += columnGap;
        //    }
        //}

        //list_scrollbarVisible = contentHeight > list.viewHeight;
        list.numItems = targetNumber;
        list_hz.numItems = targetNumber;
    }
}
