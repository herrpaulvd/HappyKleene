using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyKleene
{
    // [интерпретатор] Исключение неожиданного символа в лексическом анализе
    class UnexpectedSymbolException : Exception
    {
        public readonly int Where;

        public UnexpectedSymbolException(int where)
            : base(string.Format("Unexpected symbol at position {0} in file", where))
        {
            Where = where;
        }
    }
}
