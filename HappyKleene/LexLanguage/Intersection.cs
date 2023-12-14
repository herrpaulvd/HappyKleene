using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyKleene.LexLanguage
{
    // выражение - пересечение
    class Intersection : IExpression
    {
        private List<IExpression> items; // операнды

        public Intersection(IExpression a, IExpression b)
            => items = new List<IExpression> { a, b };

        // оптимизация пересечения
        // если пересекаем с пересечением, то просто добавляем операнды other к нашим
        // иначе добавляем сам other как операнд
        public override IExpression IntersectWith(IExpression other)
        {
            if (other is Intersection i)
                items.AddRange(i.items);
            else
                items.Add(other);
            return this;
        }

        public override FSMFactory BuildFSM()
        {
            var result = FSMFactory.BuildIntersection(items[0].BuildFSM(), items[1].BuildFSM());
            for (int i = 2; i < items.Count; i++)
                result = FSMFactory.BuildIntersection(result, items[i].BuildFSM());
            return result;
        }
    }
}
