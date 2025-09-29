using System;
using System.Text;
using System.Text.RegularExpressions;
using FairyGUI.Foundations.Number;
using UnityEngine;
using static System.Math;
using static FairyGUI.Extensions.NumberInputFoundation;
using static FairyGUI.Foundations.Arithmetic.ArithmeticUtil;

namespace FairyGUI.Extensions
{
    internal static class NumberInputFoundation
    {
        public const string numberInputRestrict = @"[0-9.e\-\+\*/%\^\(\)]";
        public static readonly Lazy<Regex> numberInputRestrictPattern = new Lazy<Regex>(() =>
        new Regex(numberInputRestrict, RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromMilliseconds(50)));

        public static readonly double Epsilon;

        static NumberInputFoundation()
        {
            Epsilon = double.Epsilon;
            if (Epsilon == 0)  // ARM
            {
                //https://learn.microsoft.com/zh-cn/dotnet/fundamentals/runtime-libraries/system-double-epsilon
                Epsilon = 2.2250738585072014E-308;
            }
        }

        public static bool EnsureTextAnyNotDigit(string value, bool integer)
        {
            for (int i = 0; i < value.Length && value[i] != 0; i++)
            {
                switch (value[i])
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        break;
                    case '.':
                        if (integer) { return false; }
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        public static void AppendUntilEmpty(StringBuilder sb, string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == 0)
                {
                    sb.Append(str, 0, i);
                    return;
                }
            }
            sb.Append(str);
        }

        public static ReadOnlySpan<char> SpanUntilEmpty(string str)
        {
            int p = 0;
            for (; p < str.Length; p++)
            {
                if (str[p] == 0)
                {
                    break;
                }
            }

            return str.AsSpan(0, p);
        }
    }

    /// <summary>
    /// 数字输入框控件的设置项目，在 FairyGUI 编辑器中为控件的用户资源填写对应的 Json 即可实现初始化
    /// </summary>
    public struct NumberInputUserMessage : IEquatable<NumberInputUserMessage>
    {
        /// <summary>
        /// 左右拖拽鼠标时每移动 1 像素改变的值
        /// </summary>
        public float dx;
        /// <summary>
        /// 输入框表达整数或者小数
        /// </summary>
        public bool integer;
        /// <summary>
        /// 如果显示浮点数，控制小数点后位数
        /// </summary>
        public int roundDigits;

        public readonly void Deconstruct(out float dx, out bool integer, out int roundDigits)
        {
            dx = this.dx;
            integer = this.integer;
            roundDigits = this.roundDigits;
        }

        public readonly bool Equals(NumberInputUserMessage other)
        {
            return other.dx == dx
                && other.integer == integer
                && other.roundDigits == roundDigits;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is NumberInputUserMessage other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(dx, integer, roundDigits);
        }

        public static bool operator ==(NumberInputUserMessage left, NumberInputUserMessage right) => left.Equals(right);
        public static bool operator !=(NumberInputUserMessage left, NumberInputUserMessage right) => !left.Equals(right);
    }

    /// <summary>
    /// 要定制新的数字输入框控件，使控件类继承此接口
    /// </summary>
    public interface INumberInputObject
    {
        NumberInputUserMessage CustomOptions { get; set; }
        bool DragValueEnabled { get; set; }
#pragma warning disable IDE1006 // 命名样式
        EventListener onSubmit { get; }
        EventListener onValueChanged { get; }
        string text { get; set; }
        public double min { get; set; }
        public double max { get; set; }
        public bool selectAllOnFocus { get; set; }
#pragma warning restore IDE1006 // 命名样式
        GObject TitleObject { get; }
        double Value { get; set; }

        void SetSelection(int start, int length);
        void StopTouch();
    }

    /// <summary>
    /// 数字输入框控件的主要行为。使用独立类型是因为 FairyGUI
    /// 可以作为输入框使用的控件类型不唯一也没有继承关系（Label，按钮，组件等）
    /// 要定制新的数字输入框控件，使控件类继承 <see cref="INumberInputObject"/>
    /// 再持有一个此类型的字段，使控件实现包装此类型的实例。
    /// <code>
    /// 组件：
    /// contentPane => title, handle
    /// title => 输入文本
    /// handle => 图形等元件, 需要可以接受拖拽
    /// </code>
    /// </summary>
    public sealed class NumberInputObject : INumberInputObject
    {
        NumberInputUserMessage userMessage;

        Vector2 coordOnTouchBegin;
        int touchId;

#if UNITY_STANDALONE_WIN
        RECT windowRectOnTouchBegin;
#endif

        double _value;
        bool _textDirty = true;

        double valueOnTouchBegin;
        int _touchSelectCounter = 0;

        bool _dispatchSubmit = false;

        /* [2022-12-01]
         * 拖拽 handle 有时也会使输入框进入焦点，但输入框进出焦点总是慢一帧。
         * 靠两个标记约束时序，防止拖拽 handle 结束触发额外的提交事件
         */
        bool _handleTouchBegin = false;
        bool _titleFocus = false;

        readonly GComponent contentPane;
        readonly Func<GComponent, GObject> getTitleObject;
        IntegerStringInstance integerStringInstance;
        FloatStringInstance floatStringInstance;

        GObject _titleObject;
        GObject _handle;
        Controller focus;

        EventListener _onValueChanged;
        EventListener _onSubmit;

        public NumberInputObject(GComponent contentPane)
        {
            this.contentPane = contentPane;
        }

        public void ConstructFromXML()
        {
            _handle = contentPane.GetChild("handle");
            if (_handle != null)
            {
                _handle.cursor = "drag-ew";  // 应用初始化在 FGUI 注册
                _handle.onTouchBegin.Add(Handle_onTouchBegin);
                _handle.onTouchEnd.Add(Handle_onTouchEnd);
            }

            focus = contentPane.GetController("focus");
            _titleObject = contentPane.GetChild("title");
            var _input = _titleObject.asTextInput;
            _input.onFocusIn.Add(TitleObj_onFocusIn);
            _input.onFocusOut.Add(TitleObj_onFocusOut);
            contentPane.onTouchBegin.Add(Self_onTouchBegin);
        }

        public void Setup_AfterAdd()
        {
            userMessage = contentPane.data is string userData
                ? JsonUtility.FromJson<NumberInputUserMessage>(userData)
                : new NumberInputUserMessage { dx = 1 };

            if (TitleObject is GTextInput _input)
            {
                //_input.restrict = numberInputRestrict;
                _input.inputTextField.InternalSetRestrict(numberInputRestrict, numberInputRestrictPattern.Value);
            }
        }

        public void OnUpdate()
        {
            if (_handleTouchBegin)
            {
                Handle_DoOnTouch();
            }

            if (_dispatchSubmit)
            {
                _dispatchSubmit = false;

                _onValueChanged?.Call();
                _onSubmit?.Call();
            }
        }

        public void SetSelection(int start, int length)
        {
            var _input = TitleObject.asTextInput;
            _input.SetSelection(start, length);
        }

        public void StopTouch()
        {
            if (_handleTouchBegin)
            {
                // on touch end
                if (Value != valueOnTouchBegin)
                {
                    //Debug.Log("number input submit");
                    _dispatchSubmit = true;
                }

                _handleTouchBegin = false;
            }
        }

        public NumberInputUserMessage CustomOptions
        {
            get => userMessage;
            set
            {
                // 输入合法性检查
                if (!value.integer)
                {
                    value.roundDigits = Clamp(value.roundDigits, 0, 15);
                }

                // 如果浮点数显示精度改变，要重新分配缓冲区
                if (floatStringInstance != null && value.roundDigits != userMessage.roundDigits)
                {
                    floatStringInstance = null;
                }

                userMessage = value;
                if (TitleObject is GTextInput _input)
                {
                    //_input.restrict = numberInputRestrict;
                    _input.inputTextField.InternalSetRestrict(numberInputRestrict, numberInputRestrictPattern.Value);
                }
            }
        }

        public GObject TitleObject => _titleObject ??= getTitleObject(contentPane);

        /// <summary>
        /// 允许在输入框附近拖拽赋值
        /// </summary>
        public bool DragValueEnabled
        {
            get => _handle != null && _handle.touchable;
            set
            {
                if (_handle != null)
                {
                    _handle.touchable = value;
                }
            }
        }

        public double Value
        {
            get => _value;
            set
            {
                if (_textDirty
                    || string.IsNullOrEmpty(text)
                    || Abs(value - _value) > NumberInputFoundation.Epsilon
                    || !EnsureTextAnyNotDigit(text, userMessage.integer))
                {
                    _value = userMessage.integer ? Round(value) : value;
                    _value = _value < min ? min : _value > max ? max : _value;

                    if (!_titleFocus)
                    {
                        //text = $"{_value:0.####}";
                        if (userMessage.integer)
                        {
                            long vint = (long)_value;
                            text = null;
                            integerStringInstance ??= IntegerStringInstance.Setup();
                            text = integerStringInstance.Modify(vint);
                        }
                        else
                        {
                            text = null;
                            floatStringInstance ??= FloatStringInstance.Setup(rightBufferLength: userMessage.roundDigits);
                            text = floatStringInstance.Modify(_value);

                            //{
                            //    string instText = floatStringInstance.AsSpan().ToString();
                            //    string actual = _value.ToString("0.######");
                            //    Debug.Log($"{instText} vs {actual} ({_value})");
                            //    Assert.AreEqual(instText, actual);
                            //}
                        }
                        _textDirty = false;
                    }
                }
            }
        }

        public double min { get; set; } = 0;
        public double max { get; set; } = 100;

        public string text
        {
            get => TitleObject.text; set
            {
                TitleObject.text = value;
                _textDirty = true;
            }
        }

        public bool selectAllOnFocus { get; set; } = false;

        public EventListener onValueChanged => _onValueChanged ??= new EventListener(contentPane, "onValueChanged");
        public EventListener onSubmit => _onSubmit ??= new EventListener(contentPane, "onSubmit");


        void Self_onTouchBegin()
        {
            if (selectAllOnFocus && _touchSelectCounter == 0)
            {
                SetSelection(0, text.Length);
            }
            unchecked
            {
                ++_touchSelectCounter;
            }
        }

        void Handle_onTouchBegin(EventContext e)
        {
            if (e.inputEvent.button == 0)
            {
                touchId = e.inputEvent.touchId;
                e.CaptureTouch();
#if UNITY_STANDALONE_WIN  
                User32.GetCursorPos(out var coord);
                User32.GetMonitorRectByCursorCoord(coord, out windowRectOnTouchBegin);
                coordOnTouchBegin = coord;
#else
                coordOnTouchBegin = e.inputEvent.position;
#endif
                valueOnTouchBegin = Value;
                _handleTouchBegin = true;
            }
        }

        void Handle_DoOnTouch()
        {
            float deltaX;
#if UNITY_STANDALONE_WIN
            User32.RoundCursor(windowRectOnTouchBegin, out var currentCoord, out int dx, out _);
            coordOnTouchBegin.x += dx;
            deltaX = currentCoord.x - coordOnTouchBegin.x;
#else
            var currentCoord = Stage.inst.GetTouchPosition(touchId);
            deltaX = currentCoord.x - coordOnTouchBegin.x;
#endif
            Value = valueOnTouchBegin + deltaX * userMessage.dx;
            _onValueChanged?.Call();
        }

        void Handle_onTouchEnd(EventContext e)
        {
            if (_handleTouchBegin && e.inputEvent.button == 0)
            {
                if (Value != valueOnTouchBegin)
                {
                    //Debug.Log("number input submit");
                    _dispatchSubmit = true;
                }
                //onFocusOut.Call();
            }
            _handleTouchBegin = false;
        }

        void TitleObj_onFocusOut(EventContext e)
        {
            var sender = (GTextInput)e.sender;
            try
            {
                if (_titleFocus)
                {
                    _titleFocus = false;

                    var sp = SpanUntilEmpty(sender.text);
                    // 文本框里可能什么都没有，尝试恢复上一次输入
                    Value = sp.Length == 0
                        ? _value
                        : Eval(sp);

                    _dispatchSubmit = true;
                }
            }
            catch (ArithmeticException err)
            {
                if (Debug.isDebugBuild) { Debug.LogError($"{nameof(NumberInputObject)}::{err.Message}"); }
                // 遇到解析错误，尝试恢复上一次输入
                Value = _value;
            }

            if (focus != null) { focus.selectedIndex = 0; }
            _touchSelectCounter = 0;
        }

        void TitleObj_onFocusIn()
        {
            if (focus != null) { focus.selectedIndex = 1; }
            if (!_handleTouchBegin) { _titleFocus = true; }
        }
    }
}
