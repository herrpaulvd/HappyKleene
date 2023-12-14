using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace HappyKleene.LexLanguage
{
    // выражение - объединение
    class Union : IExpression
    {
        private List<IExpression> items; // операнды

        public Union(IExpression a, IExpression b)
            => items = new List<IExpression> { a, b };

        public override IExpression UnionWith(IExpression other)
        {
            if (other is Union u)
                items.AddRange(u.items);
            else
                items.Add(other);
            return this;
        }

        public override FSMFactory BuildFSM()
        {
            var result = FSMFactory.BuildUnion(items[0].BuildFSM(), items[1].BuildFSM());
            for (int i = 2; i < items.Count; i++)
                result = FSMFactory.BuildUnion(result, items[i].BuildFSM());
            return result;
        }
    }
}
