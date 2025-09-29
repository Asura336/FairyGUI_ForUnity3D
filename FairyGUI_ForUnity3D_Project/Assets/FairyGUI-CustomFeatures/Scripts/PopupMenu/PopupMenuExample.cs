using System;
using FairyGUI;
using FairyGUI.Extensions;
using UnityEngine;

namespace Scripts
{
    public class PopupMenuExample : MonoBehaviour
    {
        PopupMenuEnvironment env;
        Func<WindowsLikePopupMenu> showMenu;
        PopupMenuBuilder menuBuilder;

        public bool toggle = false;
        public bool menuAutoSize = true;
        [Range(5, 20)]
        public int menuVisibleNumber = 10;

        private void Awake()
        {
            UIPackage.AddPackage("UI/CustomFeatures");

            UIObjectFactory.SetPackageItemExtension("ui://CustomFeatures/CommonPopupMenu_item", typeof(WindowsLikePopupMenuItem));
        }

        private void Start()
        {
            env = new PopupMenuEnvironment(resourceURL: "ui://CustomFeatures/CommonPopupMenu",
                separatorURL: "ui://CustomFeatures/CommonPopupMenu_separator",
                labelURL: "ui://CustomFeatures/CommonPopupMenu_label");
            env.onException = e =>
            {
                Debug.LogException(e);
            };

            static void onClickItem(WindowsLikePopupMenuItem item, object status)
            {
                var sender = item;
                Debug.Log($"{sender.text} clicked");
            }

            // a: 通过 PopupMenuEnvironment 直接构建
            showMenu = env.BuildMenuFunction(
                (e, m) => e.AppendNormalItem(m, "普通项", null, onClickItem),
                (e, m) => e.AppendCheckableItem(m, "勾选项", toggle, static (i, o) => ((PopupMenuExample)o).toggle = !((PopupMenuExample)o).toggle, this),
                (e, m) => e.AppendNextMenuItem(m, "子选项", null, nextMenu(e))
                );

            static Func<WindowsLikePopupMenu> nextMenu(PopupMenuEnvironment e) => e.BuildMenuFunction(
                (e, m) => e.AppendNormalItem(m, "子选项 1", null, onClickItem));

            // b: 通过 PopupMenuBuilder，可以组合
            var mb = new PopupMenuBuilder()
                .AppendLabel(title: "[color=#2234DD]标题[/color]", icon: null, expanded: false)
                .AppendSeparator()
                .AppendNormalItem("普通项", onClick: onClickItem)
                .AppendNormalItem("普通项", onClick: onClickItem, remark: "remark")
                .AppendCheckableItem("勾选项", () => toggle, () => toggle = !toggle);

            var mb1_next = new PopupMenuBuilder()
                .AppendNormalItem("子选项 1", onClick: onClickItem)
                .AppendNormalItem("子选项 2", onClick: onClickItem)
                .AppendNormalItem("子选项 3", onClick: onClickItem)
                .AppendNormalItem("子选项 4", onClick: onClickItem)
                .AppendNormalItem("子选项 5", onClick: onClickItem)
                .AppendNormalItem("子选项 6", onClick: onClickItem)
                .AppendNormalItem("子选项 7", onClick: onClickItem)
                .AppendNormalItem("子选项 8", onClick: onClickItem)
                .AppendNormalItem("子选项 9", onClick: onClickItem)
                .AppendNormalItem("子选项 10", onClick: onClickItem)
                .AppendNormalItem("子选项 11", onClick: onClickItem)
                .AppendNormalItem("子选项 12", onClick: onClickItem)
                .AppendNormalItem("子选项 13", onClick: onClickItem)
                .AppendNormalItem("子选项 14", onClick: onClickItem)
                .AppendNormalItem("子选项 15", onClick: onClickItem)
                .AppendNormalItem("子选项 16", onClick: onClickItem)
                .AppendNormalItem("子选项 17", onClick: onClickItem);
            var mb1 = new PopupMenuBuilder()
                .AppendNextMenuItem("子选项", null, mb1_next);

            var mb2 = new PopupMenuBuilder()
                .AppendNextMenuItem("子选项", null,
                    new PopupMenuBuilder()
                    .AppendNormalItem("普通项普通项普通项普通项普通项普通项普通项", onClick: onClickItem, remark: "remarkremarkremarkremark"));

            var mb3 = new PopupMenuBuilder()
                .AppendNextMenuItem("子选项", null,
                    new PopupMenuBuilder()
                    .AppendNormalItem("普通项", onClick: (Action)null, remark: "rrr")
                    .AppendCheckableItem("勾选项", () => toggle, () => toggle = !toggle, remark: "sss")
                    .AppendNextMenuItem("子选项1", null,
                        new PopupMenuBuilder()
                        .AppendNormalItem("DDD", onClick: onClickItem, remark: "rmk")));

            menuBuilder = mb + PopupMenuBuilder.Separator
                + mb1 + PopupMenuBuilder.Separator
                + mb2 + PopupMenuBuilder.Separator
                + mb3;
        }

        private void Update()
        {
            // 点击动作会自动移除所有弹出（Popup）的控件
            // 右键点击的菜单应该在按键松开时触发
            if (Input.GetMouseButtonUp(1))
            {
                if (GRoot.inst.touchTarget is null)
                {
                    //showMenu().Show();
                    menuBuilder.Show(env, autoSize: menuAutoSize, visibleItemNumber: menuVisibleNumber);
                }
            }
        }
    }
}