using FairyGUI.Utils;

namespace FairyGUI.Extensions
{
    /// <summary>
    /// 带有拖拽头的数字输入框，可以指定拖拽距离和变化的关系
    /// </summary>
    public class NumberInputButton : GButton, INumberInputObject
    {
        public NumberInputUserMessage CustomOptions
        {
            get => numberInput.CustomOptions;
            set => numberInput.CustomOptions = value;
        }

        public GObject TitleObject => _titleObject;

        public double min { get => numberInput.min; set => numberInput.min = value; }
        public double max { get => numberInput.max; set => numberInput.max = value; }

        public double Value
        {
            get => numberInput.Value;
            set => numberInput.Value = value;
        }

        public override string text
        {
            get => numberInput.text;
            set => numberInput.text = value;
        }
        public bool selectAllOnFocus { get; set; } = false;
        public EventListener onValueChanged => numberInput.onValueChanged;
        public EventListener onSubmit => numberInput.onSubmit;


        NumberInputObject numberInput;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            numberInput = new NumberInputObject(this);
            numberInput.ConstructFromXML();
        }

        public override void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_AfterAdd(buffer, beginPos);
            numberInput.Setup_AfterAdd();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            numberInput.OnUpdate();
        }

        public void SetSelection(int start, int length)
        {
            numberInput.SetSelection(start, length);
        }

        public void StopTouch()
        {
            numberInput.StopTouch();
        }

        /// <summary>
        /// 允许在输入框附近拖拽赋值
        /// </summary>
        public bool DragValueEnabled
        {
            get => numberInput.DragValueEnabled;
            set => numberInput.DragValueEnabled = value;
        }
    }
}