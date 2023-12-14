using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HappyKleene
{
    // класс контекстно-свободной грамматики
    // класс работает с абстрактными терминалами и нетерминалами,
    // каждый представитель которых имеет символическое строковое имя 
    // и целочисленный идентификатор
    class Grammar
    {
        private List<string> symbolNames = new List<string>(); // функция получения имени по идентификатору
        private List<bool> terminal = new List<bool>(); // является ли символ терминалом
        private SortedDictionary<string, int> symbolIds = new SortedDictionary<string, int>(); // функция получения идентификатора по имени
        private List<(int, int[])> rules = new List<(int, int[])>(); // правила как пара (int src, int[] body)
                                                                     // src - id левой части правила
                                                                     // body - массив id символов правой части
        private SortedDictionary<int, List<int>> sortedRuleIds = new SortedDictionary<int, List<int>>(); // номера правил по src
        public int Start { get; private set; } // стартовый нетерминал

        public Grammar(string start)
        {
            AddNonTerminal(start);
            Start = symbolIds[start];
        }

        // регистрация терминала
        public void AddTerminal(string t)
        {
            if (symbolIds.ContainsKey(t))
            {
                if (terminal[symbolIds[t]]) return;
                throw new Exception(string.Format("There's a nonterminal with the same name: {0}", t));
            }

            int id = symbolNames.Count;
            symbolNames.Add(t);
            terminal.Add(true);
            symbolIds.Add(t, id);
        }

        // регистрация нетерминала
        public void AddNonTerminal(string nt)
        {
            if (symbolIds.ContainsKey(nt))
            {
                if (!terminal[symbolIds[nt]]) return;
                throw new Exception(string.Format("There's a nonterminal with the same name: {0}", nt));
            }

            int id = symbolNames.Count;
            symbolNames.Add(nt);
            terminal.Add(false);
            symbolIds.Add(nt, id);
        }

        // регистрация правила по src, body и ID правила
        public void AddRule(string src, IEnumerable<string> body, int ruleID)
        {
            int srcId = symbolIds[src];
            int[] prodIds = body.Select(i => symbolIds[i]).ToArray();

            if (terminal[srcId])
                throw new BuildingException("Terminal at the left of a rule", ruleID);

            rules.Add((srcId, prodIds));

            if (!sortedRuleIds.ContainsKey(srcId))
                sortedRuleIds.Add(srcId, new List<int>());
            sortedRuleIds[srcId].Add(rules.Count - 1);
        }

        // написать устранение недостижимых и ненужных символов
        // null если язык пуст; в противном случае - новая грамматика
        public Grammar MakeBetter()
        {
            // used по окончании процедуры имеет три состояния
            // 0 - бесполезные символы
            // 1 - недостижимые полезные символы
            // 2 - символы, которые мы оставляем в грамматике

            //1) бесполезные символы
            // объявим полезными все терминалы
            int[] used = terminal.Select(b => b ? 1 : 0).ToArray();

            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var r in rules)
                {
                    if (used[r.Item1] == 1) continue;
                    if (r.Item2.All(x => used[x] == 1))
                    {
                        used[r.Item1] = 1;
                        changed = true;
                    }
                }
            }

            if (used[Start] == 0) return null; // пустой язык

            //2) недостижимые символы
            used[Start] = 2;

            Queue<int> q = new Queue<int>();
            q.Enqueue(Start);

            while (q.Count > 0)
            {
                int u = q.Dequeue();

                foreach (var rIndex in sortedRuleIds[u])
                    foreach (var c in rules[rIndex].Item2)
                        if (used[c] == 1)
                        {
                            used[c] = 2;
                            if (!terminal[c])
                                q.Enqueue(c);
                        }
            }

            // 3) отсеивание правил для новой грамматики
            Grammar res = new Grammar(symbolNames[Start]);

            for (int i = 0; i < used.Length; i++)
                if (terminal[i]) // добавляем все терминалы, это важно!
                    res.AddTerminal(symbolNames[i]);
                else if (used[i] == 2)
                    res.AddNonTerminal(symbolNames[i]);
                

            foreach (var r in rules)
            {
                if (used[r.Item1] == 2 && r.Item2.All(x => used[x] == 2))
                    res.AddRule(symbolNames[r.Item1], r.Item2.Select(x => symbolNames[x]), 0);
            }

            return res;
        }

        // интерфейс просмотра правил
        // индексы
        public int RuleCount => rules.Count;
        // правило из всех правил по индексу
        public (int, int[]) this[int i] => rules[i];
        public bool IsTerminal(int i) => terminal[i];
        public string GetName(int id) => symbolNames[id];
        public int GetId(string name) => symbolIds[name];

        public IEnumerable<int> RuleIdsBySource(int src)
            => sortedRuleIds[src];

        // методы компиляции

        // используемые типы из mscorlib.dll
        private static readonly Type t_bool = typeof(bool);
        private static readonly Type t_int = typeof(int);
        private static readonly Type t_array_int = typeof(int[]);
        private static readonly Type t_sortedRuleIds = typeof(SortedDictionary<int, int[]>);
        private static readonly Type t_string = typeof(string);
        private static readonly Type t_dict_string_int = typeof(SortedDictionary<string, int>);

        // используемые методы и конструкторы
        private static readonly ConstructorInfo ctor_sortedRuleIds = t_sortedRuleIds.GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo method_sortedRuleIds_Add = t_sortedRuleIds.GetMethod("Add", new Type[] { t_int, t_array_int });
        private static readonly ConstructorInfo ctor_dict = t_dict_string_int.GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo method_dict_Add = t_dict_string_int.GetMethod("Add", new Type[] { t_string, t_int });

        [Obsolete("Serialization version must be used")]
        public void EmitTerminal(ILGenerator il)
        {
            // создание
            il.Emit(OpCodes.Ldc_I4, terminal.Count); // length
            il.Emit(OpCodes.Newarr, t_bool); // terminal
            for(int i = 0; i < terminal.Count; i++)
            {
                il.Emit(OpCodes.Dup); // t, t
                il.Emit(OpCodes.Ldc_I4, i); // t, t, i
                il.Emit(OpCodes.Ldc_I4, terminal[i] ? 1 : 0); // t, t, i, terminal[i]
                il.Emit(OpCodes.Stelem, t_bool); // t
            }
            // terminal на стеке
        }

        public void EmitTerminal(ILGenerator il, MethodInfo m_de_ib)
        {
            var sb = new StringBuilder();
            SerializationCompiler.SerializeArray(terminal.Select(e => e ? 1 : 0), sb);
            SerializationCompiler.EmitSerializationLoading(il, sb.ToString()); // str
            il.Emit(OpCodes.Callvirt, SerializationCompiler.m_GetEnum); // se
            il.Emit(OpCodes.Call, m_de_ib); // terminal
        }

        [Obsolete("Serialization version must be used")]
        public void EmitRuleSources(ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4, rules.Count); // length
            il.Emit(OpCodes.Newarr, t_int); // rulesources
            for (int i = 0; i < rules.Count; i++)
            {
                il.Emit(OpCodes.Dup); // rs, rs
                il.Emit(OpCodes.Ldc_I4, i); // rs, rs, i
                il.Emit(OpCodes.Ldc_I4, rules[i].Item1); // rs, rs, i, rs[i]
                il.Emit(OpCodes.Stelem, t_int); // rs
            }
            // rulesources на стеке
        }

        public void EmitRuleSources(ILGenerator il, MethodInfo m_de_ia)
        {
            var sb = new StringBuilder();
            SerializationCompiler.SerializeArray(rules.Select(r => r.Item1), sb);
            SerializationCompiler.EmitSerializationLoading(il, sb.ToString()); // str
            il.Emit(OpCodes.Callvirt, SerializationCompiler.m_GetEnum); // se
            il.Emit(OpCodes.Call, m_de_ia); // rule-srcs
        }

        [Obsolete("Serialization version must be used")]
        public void EmitRuleBodies(ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4, rules.Count); // length
            il.Emit(OpCodes.Newarr, t_array_int); // rulebodies
            for(int i = 0; i < rules.Count; i++)
            {
                il.Emit(OpCodes.Dup); // rb, rb
                il.Emit(OpCodes.Ldc_I4, i); // rb, rb, i
                il.Emit(OpCodes.Ldc_I4, rules[i].Item2.Length); // rb, rb, i, length
                il.Emit(OpCodes.Newarr, t_int); // rb, rb, i, rb[i]
                for(int j = 0; j < rules[i].Item2.Length; j++)
                {
                    il.Emit(OpCodes.Dup); // rb, rb, i, rb[i], rb[i]
                    il.Emit(OpCodes.Ldc_I4, j); // rb, rb, i, rb[i], rb[i], j
                    il.Emit(OpCodes.Ldc_I4, rules[i].Item2[j]); // rb, rb, i, rb[i], rb[i], j, e
                    il.Emit(OpCodes.Stelem, t_int); // rb, rb, i, rb[i]
                }
                il.Emit(OpCodes.Stelem, t_array_int); // rb
            }
            // rulebodies на стеке
        }

        public void EmitRuleBodies(ILGenerator il, MethodInfo m_de_iaa)
        {
            var sb = new StringBuilder();
            SerializationCompiler.SerializeIntAA(rules.Select(r => r.Item2), sb);
            SerializationCompiler.EmitSerializationLoading(il, sb.ToString()); // str
            il.Emit(OpCodes.Callvirt, SerializationCompiler.m_GetEnum); // se
            il.Emit(OpCodes.Call, m_de_iaa); // rule-bodies
        }

        [Obsolete("Serialization version must be used")]
        public void EmitSortedRuleIds(ILGenerator il)
        {
            il.Emit(OpCodes.Newobj, ctor_sortedRuleIds); // sri
            foreach(var pair in sortedRuleIds)
            {
                int i = pair.Key;
                List<int> rules = pair.Value;
                il.Emit(OpCodes.Dup); // sri, sri
                il.Emit(OpCodes.Ldc_I4, i); // sri, sri, i
                il.Emit(OpCodes.Ldc_I4, rules.Count); // sri, sri, i, length
                il.Emit(OpCodes.Newarr, t_int); // sri, sri, i, sri[i]
                for(int j = 0; j < rules.Count; j++)
                {
                    il.Emit(OpCodes.Dup); // sri, sri, i, sri[i], sri[i]
                    il.Emit(OpCodes.Ldc_I4, j); // sri, sri, i, sri[i], sri[i], j
                    il.Emit(OpCodes.Ldc_I4, rules[j]); // sri, sri, i, sri[i], sri[i], j, e
                    il.Emit(OpCodes.Stelem, t_int); // sri, sri, i, sri[i]
                }
                il.Emit(OpCodes.Call, method_sortedRuleIds_Add); // sri
            }

            // sortedRuleIds на стеке
        }

        public void EmitSortedRuleIds(ILGenerator il, MethodInfo m_de_dict)
        {
            var sb = new StringBuilder();
            SerializationCompiler.SerializeDict(sortedRuleIds, sb, (c, s) => s.Append(c), SerializationCompiler.SerializeArray);
            SerializationCompiler.EmitSerializationLoading(il, sb.ToString()); // str
            il.Emit(OpCodes.Callvirt, SerializationCompiler.m_GetEnum); // se
            il.Emit(OpCodes.Call, m_de_dict); // rule-ids
        }

        public void EmitStart(ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4, Start);
        }

        [Obsolete("Serialization version must be used")]
        public void EmitSymbolNames(ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4, symbolNames.Count); // length
            il.Emit(OpCodes.Newarr, t_string); // symbolnames
            for(int i = 0; i < symbolNames.Count; i++)
            {
                il.Emit(OpCodes.Dup); // sn, sn
                il.Emit(OpCodes.Ldc_I4, i); // sn, sn, i
                il.Emit(OpCodes.Ldstr, symbolNames[i]); // sn, sn, i, s
                il.Emit(OpCodes.Stelem, t_string); // sn
            }
            // symbolNames на стеке
        }

        public void EmitSymbolNames(ILGenerator il, MethodInfo m_de_sa)
        {
            var sb = new StringBuilder();
            SerializationCompiler.SerializeArray(symbolNames, sb);
            SerializationCompiler.EmitSerializationLoading(il, sb.ToString()); // str
            il.Emit(OpCodes.Callvirt, SerializationCompiler.m_GetEnum); // se
            il.Emit(OpCodes.Call, m_de_sa); // symnames
        }

        [Obsolete("Serialization version must be used")]
        public void EmitSymbolIds(ILGenerator il)
        {
            il.Emit(OpCodes.Newobj, ctor_dict); // dict
            foreach(var item in symbolIds)
            {
                il.Emit(OpCodes.Dup); // dict, dict
                il.Emit(OpCodes.Ldstr, item.Key); // dict, dict, s
                il.Emit(OpCodes.Ldc_I4, item.Value); // dict, dict, s, i
                il.Emit(OpCodes.Call, method_dict_Add); // dict
            }
            // dict на стеке
        }

        [Obsolete("Don not use")]
        public void EmitSymbolIds(ILGenerator il, MethodInfo m_de_dict)
        {
            var sb = new StringBuilder();
            SerializationCompiler.SerializeDict(symbolIds, sb, (i, s) => s.Append(i), (i, s) => s.Append(i));
            SerializationCompiler.EmitSerializationLoading(il, sb.ToString()); // str
            il.Emit(OpCodes.Callvirt, SerializationCompiler.m_GetEnum); // se
            il.Emit(OpCodes.Call, m_de_dict); // symids
        }
    }
}
