#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using Color = UnityEngine.Color;
using PopupMenuClass = FairyGUI.Extensions.WindowsLikePopupMenu;

namespace FairyGUI.Extensions
{
    /// <summary>
    /// 描述弹出菜单内容的包装类，支持内容拼接
    /// </summary>
    public sealed class PopupMenuBuilder : IEnumerable<MenuDefinition>, IReadOnlyList<MenuDefinition>
    {
        public const int defaultCapacity = 8;

        static readonly Lazy<PopupMenuBuilder> s_separator = new(
            () => new PopupMenuBuilder().Append(
                (e, m) => e.AppendSeparator(m)));
        public static PopupMenuBuilder Separator => s_separator.Value;


        readonly List<MenuDefinition> contexts;

        public PopupMenuBuilder(int capacity = defaultCapacity)
        {
            contexts = new List<MenuDefinition>(capacity);
        }

        public PopupMenuBuilder(List<MenuDefinition> menuDefinitions)
             : this(menuDefinitions.Count)
        {
            contexts.AddRange(menuDefinitions);
        }

        public PopupMenuBuilder(IEnumerable<MenuDefinition> menuDefinitions, int capacity = defaultCapacity)
            : this(capacity)
        {
            contexts.AddRange(menuDefinitions);
        }

        public PopupMenuBuilder Append(MenuDefinition menuDefinition)
        {
            contexts.Add(menuDefinition);
            return this;
        }

        /// <summary>
        /// 组合另一个菜单定义，生成新的对象
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public PopupMenuBuilder Combine(PopupMenuBuilder other)
        {
            var res = new PopupMenuBuilder(contexts.Count + other.Count);
            res.contexts.AddRange(contexts);
            res.contexts.AddRange(other);
            return res;
        }

        /// <summary>
        /// 执行构造菜单，返回可以显示的菜单
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public PopupMenuClass Invoke(PopupMenuEnvironment environment)
        {
            var ins = environment.GetEmptyInstance();
            foreach (var c in contexts)
            {
                c(environment, ins);
            }
            DisplayMenu = ins;
            return DisplayMenu;
        }

        public IEnumerator<MenuDefinition> GetEnumerator()
        {
            return contexts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)contexts).GetEnumerator();
        }

        public MenuDefinition this[int index] => contexts[index];

        public int Count => contexts.Count;

        /// <summary>
        /// 暂存显示此实例内容的菜单 ui 对象
        /// </summary>
        public PopupMenuClass? DisplayMenu { get; private set; } = null;

        public static PopupMenuBuilder operator +(PopupMenuBuilder a, PopupMenuBuilder b) => a.Combine(b);

        public static implicit operator List<MenuDefinition>(PopupMenuBuilder a) => a.contexts;
    }

    public static class PopupMenuBuilderExtensions
    {
        public static PopupMenuBuilder AppendNormalItem(this PopupMenuBuilder self, string title, string? icon = null, WindowsLikePopupMenuItemCallback? onClick = null, object? status = null, Func<bool>? enabled = null, string? remark = null)
        {
            self.Append((e, m) => e.AppendNormalItem(m, title, icon, onClick, status, enabled is null || enabled(), remark));
            return self;
        }
        public static PopupMenuBuilder AppendNormalItem(this PopupMenuBuilder self, string title, string? icon = null, Action? onClick = null, Func<bool>? enabled = null, string? remark = null)
        {
            self.Append((e, m) => e.AppendNormalItem(m, title, icon, onClick is null ? null : static (i, s) => (s as Action)?.Invoke(), onClick, enabled is null || enabled(), remark));
            return self;
        }

        public static PopupMenuBuilder AppendNormalItem(this PopupMenuBuilder self, string title, string? icon, Color iconColor, WindowsLikePopupMenuItemCallback? onClick = null, object? status = null, Func<bool>? enabled = null, string? remark = null)
        {
            self.Append((e, m) => e.AppendNormalItem(m, title, icon, iconColor, onClick, status, enabled is null || enabled(), remark));
            return self;
        }
        public static PopupMenuBuilder AppendNormalItem(this PopupMenuBuilder self, string title, string? icon, Color iconColor, Action? onClick = null, Func<bool>? enabled = null, string? remark = null)
        {
            self.Append((e, m) => e.AppendNormalItem(m, title, icon, iconColor, onClick is null ? null : static (i, s) => (s as Action)?.Invoke(), onClick, enabled is null || enabled(), remark));
            return self;
        }

        public static PopupMenuBuilder AppendCheckableItem(this PopupMenuBuilder self, string title, Func<bool> ischecked, WindowsLikePopupMenuItemCallback? onClick = null, object? status = null, Func<bool>? enabled = null, string? remark = null)
        {
            self.Append((e, m) => e.AppendCheckableItem(m, title, ischecked(), onClick, status, enabled == null || enabled(), remark));
            return self;
        }
        public static PopupMenuBuilder AppendCheckableItem(this PopupMenuBuilder self, string title, Func<bool> ischecked, Action? onClick = null, Func<bool>? enabled = null, string? remark = null)
        {
            self.Append((e, m) => e.AppendCheckableItem(m, title, ischecked(), onClick is null ? null : static (i, s) => (s as Action)?.Invoke(), onClick, enabled == null || enabled(), remark));
            return self;
        }

        public static PopupMenuBuilder AppendNextMenuItem(this PopupMenuBuilder self, string title, string? icon, PopupMenuBuilder nextMenu, Func<bool>? enabled = null, string? remark = null)
        {
            self.Append((e, m) => e.AppendNextMenuItem(m, title, icon, e.BuildMenuFunction(nextMenu), enabled == null || enabled(), remark));
            return self;
        }

        public static PopupMenuBuilder AppendNextMenuItem(this PopupMenuBuilder self, string title, string? icon, Color iconColor, PopupMenuBuilder nextMenu, Func<bool>? enabled = null, string? remark = null)
        {
            self.Append((e, m) => e.AppendNextMenuItem(m, title, icon, iconColor, e.BuildMenuFunction(nextMenu), enabled == null || enabled(), remark));
            return self;
        }

        public static PopupMenuBuilder AppendNextMenuItem(this PopupMenuBuilder self, string title, string? icon, Func<PopupMenuClass> nextMenu, Func<bool>? enabled = null, string? remark = null)
        {
            self.Append((e, m) => e.AppendNextMenuItem(m, title, icon, nextMenu, enabled == null || enabled(), remark));
            return self;
        }

        public static PopupMenuBuilder AppendNextMenuItem(this PopupMenuBuilder self, string title, string? icon, Color iconColor, Func<PopupMenuClass> nextMenu, Func<bool>? enabled = null, string? remark = null)
        {
            self.Append((e, m) => e.AppendNextMenuItem(m, title, icon, iconColor, nextMenu, enabled == null || enabled(), remark));
            return self;
        }

        public static PopupMenuBuilder AppendSeparator(this PopupMenuBuilder self)
        {
            self.Append((e, m) => e.AppendSeparator(m));
            return self;
        }

        public static PopupMenuBuilder AppendLabel(this PopupMenuBuilder self, string title, string? icon, bool expanded)
        {
            self.Append((e, m) => e.AppendLabel(m, title, icon, expanded));
            return self;
        }

        /// <summary>
        /// 按照输入参数显示菜单
        /// </summary>
        /// <param name="self">菜单声明</param>
        /// <param name="environment">生成显示控件的环境</param>
        /// <param name="visibleItemNumber">单个菜单最大呈现条目数，超过则使用滚动条</param>
        /// <param name="minHeight">单个菜单的最小高度，如果菜单中有很多分隔符导致条目变多出现滚动条，调节这个值</param>
        /// <param name="target">在目标位置展开菜单，为 null 时在鼠标位置展开菜单</param>
        /// <param name="autoSize">指示控件宽度随文本内容自适应或者固定</param>
        /// <param name="dir">指示缺省的展开菜单方向</param>
        /// <returns></returns>
        public static PopupMenuClass Show(this PopupMenuBuilder self,
            PopupMenuEnvironment environment,
            int visibleItemNumber = 10,
            GObject? target = null,
            bool autoSize = true,
            PopupDirection dir = PopupDirection.Auto)
        {
            var o = self.Invoke(environment);
            o.autoSize = autoSize;
            o.visibleItemCount = visibleItemNumber;
            o.Show(target, dir);
            return o;
        }
    }
}
