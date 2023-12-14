using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyKleene
{
    // [интепретатор] исключение ОЖИДАНИЯ конца файла
    class ExpectedEOFException : Exception
    {
        public readonly int Where;

        public ExpectedEOFException(int where)
            : base(string.Format("EOF expected at position {0} in file", where))
        {
            Where = where;
        }
    }
}
