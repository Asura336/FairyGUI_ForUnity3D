using System;
using FairyGUI.Foundations.Collections;

namespace FairyGUI.Foundations.Arithmetic
{
    interface INode : IDisposable
    {
        int Point { get; }
        double Calculate();
    }

    /// <summary>
    /// 数字
    /// </summary>
    class NumberNode : INode
    {
        public double Number { get; private set; }
        public int Point { get; set; }
        public NumberNode(double val) => Number = val;
        public double Calculate() => Number;

        public void Dispose() { }
    }

    /// <summary>
    /// 一元运算符
    /// </summary>
    abstract class UnaryNode : INode
    {
        public INode Child { get; private set; }
        public int Point { get; set; }
        public UnaryNode(INode child) => Child = child;
        public void Dispose()
        {
            Child?.Dispose();
        }
        public abstract double Calculate();
    }

    /// <summary>
    /// 取负数
    /// </summary>
    class UMinusNode : UnaryNode
    {
        public UMinusNode(INode child) : base(child) { }
        public override double Calculate() => -Child.Calculate();
    }

    /// <summary>
    /// 取正数
    /// </summary>
    class UPlusNode : UnaryNode
    {
        public UPlusNode(INode child) : base(child) { }
        public override double Calculate() => Child.Calculate();
    }

    /// <summary>
    /// 二元运算符
    /// </summary>
    abstract class MultipleNode : INode
    {
        protected readonly ListHandle<INode> children;
        protected readonly ListHandle<int> style;
        public int Point { get; set; }

        public MultipleNode(INode child)
        {
            children = ListHandle<INode>.New();
            style = ListHandle<int>.New();
            AppendChild(child);
        }
        public void Dispose()
        {
            style.Dispose();
            foreach (var child in children)
            {
                child.Dispose();
            }
            children.Dispose();
        }

        public void AppendChild(INode child, int style = 0)
        {
            children.Add(child);
            this.style.Add(style);
        }

        public abstract double Calculate();

        public int Count => children.Count;
        public INode GetChildren(int index) => children[index];
        public int GetStyle(int index) => style[index];
    }

    /// <summary>
    /// addop -> '+' | '-'
    /// </summary>
    class SumNode : MultipleNode
    {
        public SumNode(INode child) : base(child) { }

        public override double Calculate()
        {
            double result = 0;
            for (int i = 0; i < children.Count; i++)
            {
                switch (style[i])
                {
                    case 0: result += children[i].Calculate(); break;
                    case 1: result -= children[i].Calculate(); break;
                }
            }
            return result;
        }
    }

    /// <summary>
    /// mulop -> '*' | '/' | '%'
    /// </summary>
    class ProductNode : MultipleNode
    {
        public ProductNode(INode child) : base(child) { }

        public override double Calculate()
        {
            double result = 1;
            for (int i = 0; i < children.Count; i++)
            {
                switch (style[i])
                {
                    case 0: result *= children[i].Calculate(); break;
                    // 除以 0 和被 0 除的检查交给标准库
                    case 1: result /= children[i].Calculate(); break;
                    case 2: result %= children[i].Calculate(); break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// powop -> '^'
    /// </summary>
    class PowerNode : MultipleNode
    {
        public PowerNode(INode child) : base(child) { }

        public override double Calculate()
        {
            double result = children[0].Calculate();
            for (int i = 1; i < children.Count; i++)
            {
                result = Math.Pow(result, children[i].Calculate());
            }
            return result;
        }
    }
}