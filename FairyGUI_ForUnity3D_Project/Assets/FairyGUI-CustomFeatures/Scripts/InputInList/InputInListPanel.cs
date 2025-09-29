using System;
using System.Collections;
using System.Collections.Generic;
using FairyGUI;
using FairyGUI.Extensions;
using UnityEngine;

[RequireComponent(typeof(UIPanel))]
public class InputInListPanel : MonoBehaviour
{
    /* 在以前遇到了一个输入框输入汉字时其他输入框显示出汉字的拼音和反引号的问题，
     * 当时定位和 InputTextField.UpdateText() 方法有关，
     * 但是在这里无法复现出来
     */

    private void Awake()
    {
        FairyGUI.UIObjectFactory.SetPackageItemExtension("ui://TestComs/inputField", typeof(NumberInputButton));
    }

    void Start()
    {
        var panel = GetComponent<UIPanel>();
        var ui = panel.ui;

        var list = ui.GetChild("list").asList;
        list.itemRenderer = list_itemRenderer;
        list.SetVirtual();
        list.numItems = 10;
    }

    private void list_itemRenderer(int index, GObject item)
    {
         
    }
}
