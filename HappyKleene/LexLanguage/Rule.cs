using System;
using System.Collections.Generic;
using System.Text;

namespace HappyKleene.LexLanguage
{
    // класс правила лексического анализа
    class Rule
    {
        public string Left { get; private set; } // левая часть - идентификатор
        public IExpression Right { get; private set; } // правая часть - регулярное выражение

        public Rule(string left, IExpression right)
        {
            Left = left;
            Right = right;
        }

        public FSMFactory BuildFSM()
            => Right.BuildFSM().Optimize();
    }
}
