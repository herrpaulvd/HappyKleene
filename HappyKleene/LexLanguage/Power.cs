using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyKleene.LexLanguage
{
    // выражение - степень
    class Power : IExpression
    {
        private IExpression operand; // операнд
        private int n;               // степень

        public Power(IExpression operand, int n)
        {
            this.operand = operand;
            this.n = n;
        }

        // следующие свойства нетрудно доказать:

        public override IExpression GetPower(int n)
        {
            this.n *= n;
            return this;
        }

        public override FSMFactory BuildFSM()
            => operand.BuildFSM().BuildPower(n);
    }
}
