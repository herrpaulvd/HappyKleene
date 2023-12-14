using System;
using System.Collections.Generic;
using System.Text;

namespace HappyKleene.LexLanguage
{
    // выражение - замыкание Клини
    class KleeneStar : IExpression
    {
        private IExpression operand;

        public KleeneStar(IExpression operand)
            => this.operand = operand;

        // следующие свойства очевидны:

        public override IExpression GetKleenePlus()
            => this;

        public override IExpression GetKleeneStar()
            => this;

        public override IExpression GetPlusPower(int n)
            => this;

        public override IExpression GetStarPower(int n)
            => this;

        public override IExpression GetPower(int n)
            => this;

        public override FSMFactory BuildFSM()
            => operand.BuildFSM().BuildKleeneStar();
    }
}
