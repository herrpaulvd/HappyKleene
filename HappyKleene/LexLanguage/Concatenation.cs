using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace HappyKleene.LexLanguage
{
    // выражение-конкатенация произвольного количества выражений
    class Concatenation : IExpression
    {
        private List<IExpression> items; // операнды конкатенации

        public Concatenation(IExpression a, IExpression b)
            => items = new List<IExpression> { a, b };

        // оптимизация конкатенации
        // если other - конкатенация - просто добавим операнды other к операндам this
        // иначе собственно добавим other к операндам конкатенации
        public override IExpression ConcatWith(IExpression other)
        {
            if (other is Concatenation c)
                items.AddRange(c.items);
            else
                items.Add(other);

            return this;
        }

        public override FSMFactory BuildFSM()
        {
            var result = FSMFactory.BuildConcatenation(items[0].BuildFSM(), items[1].BuildFSM());
            for (int i = 2; i < items.Count; i++)
                result = FSMFactory.BuildConcatenation(result, items[i].BuildFSM());
            return result;
        }
    }
}
