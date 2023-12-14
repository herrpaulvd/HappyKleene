using System;
using System.Collections.Generic;
using System.Text;

namespace HappyKleene
{
    // исключение построения dll (или интерпретации)
    class BuildingException : Exception
    {
        public BuildingException(string msg) : base(msg) { }
        public BuildingException(string msg, int rule)
            : base(string.Format("{0} at rule #{1}", msg, rule)) { }
    }
}
