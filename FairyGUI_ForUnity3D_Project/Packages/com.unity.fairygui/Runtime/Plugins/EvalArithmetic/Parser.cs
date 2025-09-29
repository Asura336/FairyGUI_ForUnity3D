using System;

namespace FairyGUI.Foundations.Arithmetic
{
    readonly ref struct Parser
    {
        readonly Lexer lexer;

        public Parser(Lexer lexer)
        {
            this.lexer = lexer;
        }

        public INode Parse() => Expression();

        public double Calculate(INode tree) => tree.Calculate();

        public static void AssertTreeComplete(INode tree)
        {
            AssertNodeExist(tree, 0);
        }
        static void AssertNodeExist(INode root, int point)
        {
            if (root == null) { throw new ArithmeticException($"表达式结构错误，在位置 [{point}]"); }
            switch (root)
            {
                case MultipleNode multiple:
                    var count = multiple.Count;
                    for (int i = 0; i < count; i++)
                    {
                        AssertNodeExist(multiple.GetChildren(i), root.Point);
                    }
                    break;
                case UnaryNode unary:
                    AssertNodeExist(unary.Child, root.Point);
                    break;
                case NumberNode _:
                    break;
                default:
                    throw new ArithmeticException($"出现未识别的节点 {root.GetType()}，在位置 [{point}]");
            }
        }


        /*
         * expression -> term addop term | addop term
         * addop -> '+' | '-'
         * term -> power mulop power
         * mulop -> '*' | '/' | '%'
         * power -> factor powop factor
         * powop -> '^'
         * factor -> NUM | '(' expression ')'
         * 
         */


        /// <summary>
        /// expression -> term addop term | addop term
        /// </summary>
        /// <returns></returns>
        INode Expression()
        {
            var sumNode = new SumNode(Term()) { Point = lexer.GetPoint() };
            for (var token = lexer.Token;
                token == Lexer.TokenType.Plus || token == Lexer.TokenType.Minus;
                token = lexer.Token)
            {
                lexer.Accept();
                sumNode.AppendChild(Term(), token switch
                {
                    Lexer.TokenType.Plus => 0,
                    Lexer.TokenType.Minus => 1,
                    _ => 0
                });
            }
            return sumNode;
        }

        /// <summary>
        /// power mulop power
        /// </summary>
        /// <returns></returns>
        MultipleNode Term()
        {
            var productNode = new ProductNode(Power()) { Point = lexer.GetPoint() };
            for (var token = lexer.Token;
                token == Lexer.TokenType.Multiply || token == Lexer.TokenType.Devide || token == Lexer.TokenType.Remainder;
                token = lexer.Token)
            {
                lexer.Accept();
                productNode.AppendChild(Power(), token switch
                {
                    Lexer.TokenType.Multiply => 0,
                    Lexer.TokenType.Devide => 1,
                    Lexer.TokenType.Remainder => 2,
                    _ => 0
                });
            }
            return productNode;
        }

        /// <summary>
        /// power -> factor powop factor
        /// </summary>
        /// <returns></returns>
        MultipleNode Power()
        {
            var powerNode = new PowerNode(Factor()) { Point = lexer.GetPoint() };
            for (var token = lexer.Token;
                token == Lexer.TokenType.Power;
                token = lexer.Token)
            {
                lexer.Accept();
                powerNode.AppendChild(Factor());
            }
            return powerNode;
        }

        /// <summary>
        /// factor -> NUM | '(' expression ')'
        /// </summary>
        /// <returns></returns>
        INode Factor()
        {
            var token = lexer.Token;
            var point = lexer.GetPoint();
            switch (token)
            {
                case Lexer.TokenType.Number:
                    lexer.Accept();  // next
                    return new NumberNode(lexer.Number) { Point = point };
                case Lexer.TokenType.Minus:
                    lexer.Accept();
                    return new UMinusNode(Factor()) { Point = point };
                case Lexer.TokenType.Plus:
                    lexer.Accept();
                    return new UPlusNode(Factor()) { Point = point };
                case Lexer.TokenType.LSeparator:
                    lexer.Accept();
                    var exp = Expression();
                    if (lexer.Token == Lexer.TokenType.RSeparator)
                    {
                        lexer.Accept();
                    }
                    else
                    {
                        throw new ArithmeticException($"解析 {lexer} 错误, 符号：{lexer.Token}, 位置 {point}");
                    }
                    return exp;
                default:
                    return null;
            }
        }
    }
}