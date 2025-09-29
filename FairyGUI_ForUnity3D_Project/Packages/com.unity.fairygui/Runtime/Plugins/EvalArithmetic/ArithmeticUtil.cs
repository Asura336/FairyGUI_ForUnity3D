using System;

namespace FairyGUI.Foundations.Arithmetic
{
    using static Constants;

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

    public static class ArithmeticUtil
    {
        public static double Eval(string source)
        {
            return Eval(source.AsSpan());
        }

        public static double Eval(ReadOnlySpan<char> source)
        {
            AssertSeparatorBalance(source);

            using var lexer = new Lexer(source);
            var parser = new Parser(lexer);
            using var tree = parser.Parse();
            Parser.AssertTreeComplete(tree);
            return parser.Calculate(tree);
        }

        static void AssertSeparatorBalance(ReadOnlySpan<char> expression)
        {
            int counter = 0;
            for (int i = 0; i < expression.Length; i++)
            {
                switch (expression[i])
                {
                    case __leftSeparator: ++counter; break;
                    case __rightSeparator: --counter; break;
                }
            }
            if (counter != 0) { throw new ArithmeticException("左右括号数目不一致"); }
        }
    }
}