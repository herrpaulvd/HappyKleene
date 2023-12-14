using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace HappyKleene
{
    // [интерпретатор] класс дерева разбора
    class ParsingTree
    {
        public string Kind { get; private set; } // тип вершины (имя символа)
        public string TokenValue { get; private set; } // если терминал - строковое представление
        public ParsingTree[] Children { get; private set; } // если нетерминал - дети
        public int Position { get; private set; } // позиция строки, начиная с которой находится символ

        public bool IsLeaf() => !(Children is object); // является ли листом (терминалом)

        private ParsingTree(string name, string tkValue, int pos)
        {
            Kind = name;
            TokenValue = tkValue;
            Children = null;
            Position = pos;
        }

        private ParsingTree(string name, ParsingTree[] children, int pos)
        {
            Kind = name;
            Children = children;
            TokenValue = null;
            Position = pos;
        }

        // отображение поддерева с табуляцией
        // tabs - количество табуляций на уровне
        private string View(int tabs)
        {
            string tab = new string('\t', tabs);

            if (IsLeaf())
                return tab + string.Format("leaf {0} position={1} value={2}", Kind, Position, TokenValue);

            StringBuilder res = new StringBuilder();
            res.Append(tab + "node ");
            res.Append(Kind);
            res.Append(" position=");
            res.Append(Position);
            res.Append(" children={\n");
            foreach (var c in Children)
            {
                res.Append(c.View(tabs + 1));
                res.Append('\n');
            }
            res.Append(tab + '}');
            return res.ToString();
        }

        // отображение всего дерева
        public override string ToString()
            => View(0);

        // построение дерева из последовательности токенов, грамматики и анализа
        // как последовательности идентификаторов, представляющей ПРАВЫЙ РАЗБОР (только перевёрнутый, разумеется)
        public static ParsingTree Build(IEnumerable<string> tokenValues, Grammar g, IEnumerable<int> analysis)
        {
            Stack<string> tkv = new Stack<string>();
            foreach (var val in tokenValues) tkv.Push(val); // tkv есть обращение tokenValues
                                                            // это нужно сделать, т.к. мы работает именно с правым разбором
            Queue<int> ruleIds = new Queue<int>();
            foreach (var r in analysis) ruleIds.Enqueue(r); // расположение правил в виде очереди

            // рекурсивное построение дерева с паралелльным проходом по правилам
            ParsingTree _build(int sym)
            {
                int ruleID = ruleIds.Dequeue();
                int pos = ruleIds.Dequeue();

                if (ruleID == -1)
                    return new ParsingTree(g.GetName(sym), tkv.Pop(), pos);

                int[] rbody = g[ruleID].Item2;
                ParsingTree[] children = new ParsingTree[rbody.Length];
                for (int i = rbody.Length - 1; i >= 0; i--)
                    children[i] = _build(rbody[i]);
                return new ParsingTree(g.GetName(sym), children, pos);
            }

            return _build(g.Start);
        }
    }
}
