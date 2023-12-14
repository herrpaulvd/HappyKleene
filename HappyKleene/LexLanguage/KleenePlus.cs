using System;
using System.Collections.Generic;
using System.Text;

namespace HappyKleene.LexLanguage
{
    // выражение - плюс Клини
    class KleenePlus : IExpression
    {
        private IExpression operand;

        public KleenePlus(IExpression operand)
            => this.operand = operand;

        // следующие свойства очевидны:

        public override IExpression GetKleenePlus()
            => this;

        public override IExpression GetKleeneStar()
            => operand.GetKleeneStar();

        public override IExpression GetPlusPower(int n)
            => this;

        public override IExpression GetStarPower(int n)
            => operand.GetKleeneStar();

        public override FSMFactory BuildFSM()
            => operand.BuildFSM().BuildKleenePlus();
    }
}
