using System;
using System.Collections.Generic;
using System.Text;

namespace HappyKleene.LexLanguage
{
    // выражение - степень с плюсом
    class PlusPower : IExpression
    {
        private IExpression operand; // операнд
        private int n;               // степень

        public PlusPower(IExpression operand, int n)
        {
            this.operand = operand;
            this.n = n;
        }

        // следующие свойства нетрудно доказать:

        public override IExpression GetKleenePlus()
            => operand.GetKleenePlus();

        public override IExpression GetKleeneStar()
            => operand.GetKleeneStar();

        public override IExpression GetPlusPower(int n)
        {
            this.n *= n;
            return this;
        }

        public override IExpression GetStarPower(int n)
            => operand.GetStarPower(this.n * n);

        public override FSMFactory BuildFSM()
            => operand.BuildFSM().BuildPlusPower(n);
    }
}
