using System;
using System.Collections.Generic;
using System.Text;

namespace HappyKleene.LexLanguage
{
    // выражение - степень со звездой
    class StarPower : IExpression
    {
        private IExpression operand;
        private int n;

        public StarPower(IExpression operand, int n)
        {
            this.operand = operand;
            this.n = n;
        }

        // следующие свойства нетрудно доказать:

        public override IExpression GetKleenePlus()
            => operand.GetKleeneStar();

        public override IExpression GetKleeneStar()
            => operand.GetKleeneStar();

        public override IExpression GetPlusPower(int n)
        {
            this.n *= n;
            return this;
        }

        public override IExpression GetStarPower(int n)
        {
            this.n *= n;
            return this;
        }

        public override IExpression GetPower(int n)
        {
            this.n *= n;
            return this;
        }

        public override FSMFactory BuildFSM()
            => operand.BuildFSM().BuildStarPower(n);
    }
}
