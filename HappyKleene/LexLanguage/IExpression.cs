using System;
using System.Collections.Generic;
using System.Text;

namespace HappyKleene.LexLanguage
{
    // класс выражения HK-lex
    abstract class IExpression
    {
        // строит объединение с другим выражением
        public virtual IExpression UnionWith(IExpression other)
            => new Union(this, other);

        // строит пересечение с другим выражением
        public virtual IExpression IntersectWith(IExpression other)
            => new Intersection(this, other);

        // строит конкатенацию с другим выражением
        public virtual IExpression ConcatWith(IExpression other)
            => new Concatenation(this, other);

        // строит замыкание (звезду) Клини данного выражения
        public virtual IExpression GetKleeneStar()
            => new KleeneStar(this);

        // строит плюс Клини данного выражения
        public virtual IExpression GetKleenePlus()
            => new KleenePlus(this);

        // строит степень со звездой из данного выражения
        // expr +n = expr | expr ^ 2 | ... | expr ^ n
        public virtual IExpression GetPlusPower(int n)
            => new PlusPower(this, n);

        // строит степень с плюсом из данного выражения
        // expr *n = empty | expr +n
        public virtual IExpression GetStarPower(int n)
            => new StarPower(this, n);

        // строит декартову степень выражения
        public virtual IExpression GetPower(int n)
        {
            if (n == 1) return this;
            return new Power(this, n);
        }

        // метод построение недетерминированного конечного автомата-распознавателя
        // по данному выражению
        public abstract FSMFactory BuildFSM();
    }
}
