using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyKleene
{
    // [интепретатор] исключение ожидания определённых символов
    class ExpectedTokensException : Exception
    {
        private static string FormatList(string[] s)
        {
            StringBuilder result = new StringBuilder();
            result.Append(s[0]);
            foreach(var x in s.Skip(1))
            {
                result.Append(" or ");
                result.Append(x);
            }
            return result.ToString();
        }

        public readonly string[] Expectations;
        public readonly int Where;

        public ExpectedTokensException(string[] expectations, int where)
            : base(string.Format("{0} expected at position {1} in file", FormatList(expectations), where))
        {
            Expectations = expectations;
            Where = where;
        }
    }
}
