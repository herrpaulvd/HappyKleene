using System;
using System.Collections.Generic;
using System.Text;

namespace HappyKleene.LexLanguage
{
    // выражение - константная строка
    class CharSequence : IExpression
    {
        private string source; // собственно строка

        public CharSequence(string source)
            => this.source = source;

        // метод перегружен для оптимизации конкатенации с другой строкой
        public override IExpression ConcatWith(IExpression other)
        {
            if (other is CharSequence cs)
            {
                source += cs.source;
                return this;
            }
            return base.ConcatWith(other);
        }

        // метод перегружен для оптимизации конкатенации строк
        // (асимптотической)
        public override IExpression GetPower(int n)
        {
            StringBuilder result = new StringBuilder();
            while (n-- > 0) result.Append(source);
            source = result.ToString();
            return this;
        }

        public override FSMFactory BuildFSM()
            => FSMFactory.BuildString(source);
    }
}
