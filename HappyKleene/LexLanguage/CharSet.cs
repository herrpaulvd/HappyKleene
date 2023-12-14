using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyKleene.LexLanguage
{
    // класс выражения - объединения символов
    class CharSet : IExpression
    {
        private IEnumerable<char> chars;

        public CharSet(IEnumerable<char> chars)
            => this.chars = chars;

        public override FSMFactory BuildFSM()
            => FSMFactory.BuildCharSet(chars);
    }
}
