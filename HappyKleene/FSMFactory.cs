using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Data;

namespace HappyKleene
{
    // Finite-State Machine Factory
    // служит для инициализации автомата
    // состояние номер 0 всегда считается начальным
    // этот класс предназначен для производства шаблона автомата
    // и тестового запуска в режиме интепретации
    class FSMFactory
    {
        // текущие состояния недетерминированного автомата
        private SortedSet<int> states = new SortedSet<int>();
        // заключительные состояния автомата
        private SortedSet<int> finalStates = new SortedSet<int>();
        // функция переходов
        private SortedDictionary<char, List<IEnumerable<int>>> transition = new SortedDictionary<char, List<IEnumerable<int>>>();
        // алфавит автомата
        private List<char> alphabet = new List<char>();

        // количество состояний автомата (используется только при генерации автомата)
        public int StateCount { get; private set; }

        // константа - пустой массив целых чисел
        private static readonly int[] emptyarr = new int[0];

        public FSMFactory()
        {
            StateCount = 1;
            states.Add(0); // начальным состоянием автомата всегда считаем 0
        }

        public void Reset()
        {
            states.Clear();
            states.Add(0);
        }

        private List<int> helper; // вспомогательный список

        // метод эмуляции такта
        // false если нельзя совершить ни одного такта
        public bool Tact(char c)
        {
            if (!transition.ContainsKey(c))
            {
                states.Clear();
                return false;
            }

            if (helper is object) helper.Clear();
            else helper = new List<int>();

            var func = transition[c];

            foreach (var s in states)
                helper.AddRange(func[s]);

            states.Clear();
            states.UnionWith(helper);
            return states.Count > 0;
        }

        // сообщает о том, находится ли автомат в одном из финальных состояний
        public bool Final()
            => states.Any(s => finalStates.Contains(s));

        // добавление символа в алфавит
        private void CheckSymbol(char c)
        {
            if (transition.ContainsKey(c)) return;

            var cFunc = new List<IEnumerable<int>>();
            while (cFunc.Count < StateCount)
                cFunc.Add(emptyarr);

            transition.Add(c, cFunc);
            alphabet.Add(c);
        }

        // расширение количества состояний
        private void CheckCount(int count)
        {
            if (count <= StateCount) return;

            StateCount = count;

            foreach (var e in transition)
                while (e.Value.Count < count)
                    e.Value.Add(emptyarr);
        }

        // добавление переходов в функцию переходов
        private void Add(int state, char c, IEnumerable<int> res)
        {
            CheckSymbol(c);
            CheckCount(Math.Max(state, res.DefaultIfEmpty(-1).Max()) + 1);

            if (transition[c][state] is List<int> cn)
                foreach (var e in res)
                    cn.Add(e);
            else
                transition[c][state] = transition[c][state].Concat(res).ToList();
        }

        // добавление небольшого числа переходов
        private void AddStates(int state, char c, params int[] states)
        {
            Add(state, c, states);
        }

        // получение состояний
        private IEnumerable<int> GetStates(int s, char c)
        {
            if (transition.ContainsKey(c)) return transition[c][s];
            return emptyarr;
        }

        // следующие методы получают автомат по заданному регулярному выражению
        // алгоритмы см. в указанной в работе книге

        // тривиальный автомат, настроенный на принятия единственной строки
        // логика построения очевидна
        public static FSMFactory BuildString(string s)
        {
            FSMFactory m = new FSMFactory();

            for (int i = 0; i < s.Length; i++)
                m.AddStates(i, s[i], i + 1);

            m.finalStates.Add(s.Length);

            return m;
        }

        // автомат для объединения множества односимвольных строк
        public static FSMFactory BuildCharSet(IEnumerable<char> chars)
        {
            FSMFactory m = new FSMFactory();
            foreach (char c in chars)
                m.AddStates(0, c, 1);
            m.finalStates.Add(1);
            return m;
        }

        // построение автомата объединения двух регулярных выражений
        // по их автоматам
        public static FSMFactory BuildUnion(FSMFactory m1, FSMFactory m2)
        {
            FSMFactory m = new FSMFactory();

            const int q0 = 0;
            const int q1 = 1;
            int q2 = q1 + m1.StateCount;

            // поддерживаем следующую конвертацию:
            // 0 для всех автоматов - начальное состояние
            // q для m1 есть q + q1 для m
            // q для m2 есть q + q2 для m

            // delta0 includes delta1 and delta2
            foreach (var e in m1.transition)
            {
                char c = e.Key;
                var f = e.Value;

                for (int i = 0; i < f.Count; i++)
                    m.Add(q1 + i, c, f[i].MapAdd(q1));
            }

            foreach (var e in m2.transition)
            {
                char c = e.Key;
                List<IEnumerable<int>> f = e.Value;

                for (int i = 0; i < f.Count; i++)
                    m.Add(q2 + i, c, f[i].MapAdd(q2));
            }

            // forall c: delta(q0, c) = delta(q1, c) U delta(q2, c)
            foreach (var c in m.alphabet)
                m.Add(q0, c, m.GetStates(q1, c).Concat(m.GetStates(q2, c)));

            // F0 includes F1 and F2
            m.finalStates.UnionWith(m1.finalStates.MapAdd(q1));
            m.finalStates.UnionWith(m2.finalStates.MapAdd(q2));

            // q1 belongs F1 V q2 belongs F2 => q0 belongs F0
            if (m1.finalStates.Contains(0) || m2.finalStates.Contains(0))
                m.finalStates.Add(q0);

            return m;
        }

        // построение автомата конкатенации двух регулярных выражений
        // по их автоматам
        public static FSMFactory BuildConcatenation(FSMFactory m1, FSMFactory m2)
        {
            FSMFactory m = new FSMFactory();

            const int q1 = 0;
            int q2 = q1 + m1.StateCount;

            // delta0 includes delta2 and modified delta1
            // ONLY WHEN q1 == 0!!!
            foreach (var e in m1.transition)
            {
                char c = e.Key;
                var f = e.Value;

                for (int i = 0; i < f.Count; i++)
                    m.Add(i, c, f[i]);
            }

            foreach (var e in m2.transition)
            {
                char c = e.Key;
                List<IEnumerable<int>> f = e.Value;

                for (int i = 0; i < f.Count; i++)
                    m.Add(q2 + i, c, f[i].MapAdd(q2));
            }

            // включаем delta2(q2, a) где нужно
            foreach (int i in m1.finalStates)
                foreach (var c in m.alphabet)
                    m.Add(i, c, m2.GetStates(0, c).MapAdd(q2));

            // F0 incl. F2
            // q2 incl. F2 => F0 incl. F1
            m.finalStates.UnionWith(m2.finalStates.MapAdd(q2));
            if (m2.finalStates.Contains(0))
                m.finalStates.UnionWith(m1.finalStates);

            return m;
        }

        // построение автомата для замыкания Клини регулярного выражения,
        // представленного автоматом
        // в роли автомата-прообраза выступает this
        public FSMFactory BuildKleeneStar()
        {
            const int q0 = 0;
            const int q1 = 1;

            FSMFactory m = new FSMFactory();

            // delta(q0, a) = delta1(q1, a)
            foreach (var c in alphabet)
                m.Add(q0, c, GetStates(0, c).MapAdd(q1));

            // delta(q, a) incl. delta1(q1, a)
            foreach (var c in alphabet)
                for (int i = 0; i < StateCount; i++)
                    m.Add(q1 + i, c, GetStates(i, c).MapAdd(q1));

            // and for final states also delta1(q1, a)
            foreach (var s in finalStates)
                foreach (var c in alphabet)
                    m.Add(q1 + s, c, GetStates(0, c).MapAdd(q1));

            m.finalStates.UnionWith(finalStates.MapAdd(q1));
            m.finalStates.Add(q0);

            return m;
        }

        // построение автомата для плюса Клини регулярного выражения,
        // представленного автоматом
        public FSMFactory BuildKleenePlus()
        {
            // useonly q1 instead of(q0, q1)
            //q1 == 0
            // так что нет необходимости в вызовах MapAdd и конвертации состояний

            FSMFactory m = new FSMFactory();

            // delta(q, a) incl. delta1(q, a)
            foreach (var c in alphabet)
                for (int i = 0; i < StateCount; i++)
                    m.Add(i, c, GetStates(i, c));

            // for final states add delta1(q1, a)
            foreach (var s in finalStates)
                foreach (var c in alphabet)
                    m.Add(s, c, GetStates(0, c));

            // the same finalStates set
            m.finalStates.UnionWith(finalStates);

            return m;
        }

        // вспомогательный метод, определяющий автомат с функией перехода
        // для следующих трёх методов:
        private FSMFactory ProducePowerAutomatWithoutFinals(int n)
        {
            // Q0 = Q1 x 0..n-1
            // пусть это пара (q, i)
            // где q belongs Q1, 0 <= i <= n-1
            // отождествим её с числом x = q + StateCount * i

            FSMFactory m = new FSMFactory();

            // delta([q, i], a) incl. {[p, i] : delta1(q, a) incl. p}

            // добавление состояния [p, j] в delta([q,i], c)
            void qicpj(int q, int i, char c, int p, int j)
            {
                int idI = q + i * StateCount;
                int idJ = p + j * StateCount;

                m.AddStates(idI, c, idJ);
            }

            for (int q = 0; q < StateCount; q++)
                for (int i = 0; i < n; i++)
                    foreach (var c in alphabet)
                        foreach (var p in GetStates(q, c))
                            qicpj(q, i, c, p, i);

            foreach (var q in finalStates)
                for (int i = 0; i < n - 1; i++)
                    foreach (var c in alphabet)
                        foreach (var p in GetStates(0, c))
                            qicpj(q, i, c, p, i + 1);

            return m;
        }

        // построение автомата для степени выражения, представленного автоматом
        public FSMFactory BuildPower(int n)
        {
            var m = ProducePowerAutomatWithoutFinals(n);

            foreach (var q in finalStates)
                m.finalStates.Add(q + (n - 1) * StateCount);

            return m;
        }

        // построение автомата для степени с плюсом регулярного выражения,
        // представленного автоматом
        public FSMFactory BuildPlusPower(int n)
        {
            var m = ProducePowerAutomatWithoutFinals(n);

            foreach (var q in finalStates)
                for (int i = 0; i < n; i++)
                    m.finalStates.Add(q + i * StateCount);

            return m;
        }

        // построение автомата для степени со звездой регулярного выражения,
        // представленного автоматом
        public FSMFactory BuildStarPower(int n)
        {
            var m = BuildPlusPower(n);
            m.finalStates.Add(0); // единственное отличие - включение [q1, 1] в F
            return m;
        }

        // получение автомата для пересечения регулярных выражений,
        // представленных автоматами
        public static FSMFactory BuildIntersection(FSMFactory m1, FSMFactory m2)
        {
            // алгоритм заключается в следующем:
            // будем условно считать, что над строкой запускаются одновременно два распознавателя
            // один имеет состояние q1, другой - состояние q2
            // по символу c первый переходит во множество состояний Q1, второй - Q2
            // их можно объединить в единый автомат, имеющий своим состоянием (q1, q2)
            // которые по символу c переходят в Q1 x Q2.
            // Финальным будем считать такое состояние (f1, f2) автомата, что f1 есть
            // финальное состояние первого автомата, f2 - второго
            // состояние итогового автомата (q1, q2) отождествим с числом q1 * n + q2,
            // где n - число состояний второго автомата
            // стартовое состояние - (0,0) ~ 0

            int n = m2.StateCount;
            FSMFactory m = new FSMFactory();

            for(int i = 0; i < m1.StateCount; i++)
                for(int j = 0; j < m2.StateCount; j++)
                {
                    int q = i * n + j;

                    foreach(var p in m1.transition)
                    {
                        char c = p.Key;
                        foreach (var s1 in p.Value[i])
                            foreach (var s2 in m2.GetStates(j, c))
                                m.AddStates(q, c, s1 * n + s2);
                    }
                }

            foreach (var f1 in m1.finalStates)
                foreach (var f2 in m2.finalStates)
                    m.finalStates.Add(f1 * n + f2);

            return m;
        }

        // метод удаления недостижимых состояний
        private FSMFactory DeleteUnusedStates()
        {
            bool[] used = new bool[StateCount];
            used[0] = true;
            Queue<int> q = new Queue<int>();
            q.Enqueue(0);

            while (q.Count > 0)
            {
                int u = q.Dequeue();

                foreach (var c in alphabet)
                    foreach (var v in transition[c][u])
                    {
                        if (used[v]) continue;
                        used[v] = true;
                        q.Enqueue(v);
                    }
            }

            // used[u] = true <=> u достижимо из v
            // новый автомат
            FSMFactory m = new FSMFactory();

            int j = 0; // новый индекс достижимого состояния
            int[] newId = new int[StateCount];
            for (int i = 0; i < StateCount; i++)
                if (used[i]) newId[i] = j++;

            for (int i = 0; i < StateCount; i++)
                if (used[i])
                    foreach (var c in alphabet)
                        m.Add(newId[i], c, transition[c][i].Where(s => used[s]).Select(s => newId[s]));

            //финальные достижимые состояния
            m.finalStates.UnionWith(finalStates.Where(s => used[s]).Select(s => newId[s]));

            return m;
        }

        // метод оптимизации автомата, в котором все состояния достижимы из 0
        // по образу и подобию минимизации детерминированного конечного автомата
        private FSMFactory Merge2()
        {
            int[] classes = new int[StateCount];
            (string, int)[] s = new (string, int)[StateCount];

            int oldClassCount = 0;
            int currClassCount = (finalStates.Count == 0 || finalStates.Count == StateCount ? 1 : 2);

            foreach (int i in finalStates) classes[i] = 1;

            StringBuilder sb = new StringBuilder();

            while (oldClassCount < currClassCount)
            {
                oldClassCount = currClassCount;

                for (int i = 0; i < StateCount; i++)
                {
                    sb.Append(classes[i]);
                    sb.Append('#'); // отделение номера класса 

                    // проход по функции
                    // множества, соответствующие разным классам,
                    // будут отделяться |
                    // элементы множеств будут отделяться ;

                    foreach (var c in alphabet)
                    {
                        if (helper is object) helper.Clear(); // используем вспомогательное поле - список
                        else helper = new List<int>();

                        foreach (int x in transition[c][i])
                            helper.Add(classes[x]); // взяли классы выходных состояний

                        helper.Sort(); // сортировка

                        int prev = -1;
                        foreach (var x in helper)
                            if (x != prev)
                            {
                                sb.Append(x);
                                sb.Append(';');
                                prev = x;
                            }
                        sb.Append('|');
                    }

                    // строка построена
                    s[i] = (sb.ToString(), i);
                }

                // отсортируем массив строк, выделим классы
                currClassCount = 0;
                Array.Sort(s);

                for (int i = 0; i < StateCount;)
                {
                    int j = i + 1;
                    while (j < StateCount && s[j].Item1 == s[i].Item1) j++;

                    while (i < j)
                        classes[i++] = currClassCount;
                    currClassCount++;
                }
            }

            // перестройка автомата
            FSMFactory m = new FSMFactory();
            helper.Clear(); // в helper поместим вершины-представители классов
            for (int i = 0; i < currClassCount; i++) helper.Add(-1);
            for (int i = 0; i < StateCount; i++) helper[classes[i]] = i;

            SortedSet<int> mTrans = new SortedSet<int>();
            for (int i = 0; i < currClassCount; i++)
                foreach (var c in alphabet)
                {
                    mTrans.Clear();
                    mTrans.UnionWith(transition[c][helper[i]].Select(x => classes[x]));
                    m.Add(i, c, mTrans);
                }

            //достраиваем финальные состояния
            m.finalStates.UnionWith(finalStates.Select(x => classes[x]));

            return m;
        }

        // merge для находящихся в одинаковых множествах состояний одинаковой финальности
        private FSMFactory Merge3()
        {
            StringBuilder[] classes = new StringBuilder[StateCount];
            classes[0] = new StringBuilder("@");
            for (int i = 1; i < StateCount; i++)
                classes[i] = new StringBuilder();

            for (int i = 0; i < StateCount; i++)
                foreach (char c in alphabet)
                    foreach (var s in transition[c][i])
                        if(s > 0) classes[s].Append((int)c + "#" + i + "#");

            SortedDictionary<string, int> cmap = new SortedDictionary<string, int>();
            int[] eq = new int[StateCount];
            for(int i = 0; i < StateCount; i++)
            {
                string list = classes[i].ToString();
                if (cmap.ContainsKey(list))
                    eq[i] = cmap[list];
                else
                {
                    eq[i] = cmap.Count;
                    cmap.Add(list, eq[i]);
                }
            }

            // если не получилось уменьшить автомат
            if (cmap.Count == StateCount) return this;

            int n = cmap.Count;
            FSMFactory m = new FSMFactory();
            SortedSet<int>[] mtrans = new SortedSet<int>[n];
            for (int i = 0; i < n; i++) mtrans[i] = new SortedSet<int>();

            foreach(var c in alphabet)
            {
                for (int i = 0; i < n; i++) mtrans[i].Clear();
                for (int i = 0; i < StateCount; i++)
                    mtrans[eq[i]].UnionWith(transition[c][i].Select(j => eq[j]));
                for (int i = 0; i < n; i++)
                    m.Add(i, c, mtrans[i]);
            }

            foreach (var f in finalStates)
                m.finalStates.Add(eq[f]);

            // иногда из-за объединений функций переходов могут создаться новые уменьшающие ситуации
            // поэтому попробуем провести аналогичную процедуру над получившимся автоматом
            return m.Merge3();
        }

        // оптимальный автомат
        //1) удалить недостижимые
        //2) merge2
        //3) merge3
        public FSMFactory Optimize()
            => DeleteUnusedStates().Merge2().Merge3();

        // оптимизирует автомат только если количество его состояний превышает 333
        // вызов такой процедуры необходим для сжатия слишком сильно разросшихся автоматов
        // ещё внутри процедуры их построения
        //public FSMFactory MaybeOptimize()
        //{
        //    if (StateCount > 333)
        //        return Optimize();
        //    return this;
        //}

        // методы компиляции

        // константы используемых типов из mscorlib.dll
        private static readonly Type t_transition = typeof(SortedDictionary<char, int[][]>);
        private static readonly Type t_finals = typeof(SortedSet<int>);
        private static readonly Type t_array_array_int = typeof(int[][]);
        private static readonly Type t_array_int = typeof(int[]);
        private static readonly Type t_int = typeof(int);
        private static readonly Type t_char = typeof(char);

        // константы используемых методов и конструкторов
        private static readonly ConstructorInfo ctor_transition = t_transition.GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo method_transition_Add = t_transition.GetMethod("Add", new Type[] { t_char, t_array_array_int });
        private static readonly ConstructorInfo ctor_finals = t_finals.GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo method_finals_Add = t_finals.GetMethod("Add", new Type[] { t_int });

        // метод инициализации transition
        // часть кода, генерируемая этим методом, полностью получает объект transition
        // и просто оставляет его на стеке

        // old version
        [Obsolete("Serialization version must be used")]
        public void EmitTransition(ILGenerator il)
        {
            // создание объекта
            il.Emit(OpCodes.Newobj, ctor_transition); // transition
            // по символам
            foreach(var pair in transition)
            {
                il.Emit(OpCodes.Dup); // transition, transition
                char c = pair.Key;
                List<IEnumerable<int>> func = pair.Value;
                il.Emit(OpCodes.Ldc_I4, (int)c); // t, t, c
                // создание массива func : int[][]
                il.Emit(OpCodes.Ldc_I4, func.Count); // t, t, c, length
                il.Emit(OpCodes.Newarr, t_array_int); // t, t, c, func
                for(int i = 0; i < func.Count; i++)
                {
                    il.Emit(OpCodes.Dup); // t, t, c, func, func
                    il.Emit(OpCodes.Ldc_I4, i); // t, t, c, f, f, i
                    int[] output = func[i].ToArray();
                    // создание массива output
                    il.Emit(OpCodes.Ldc_I4, output.Length); // t, t, c f, f, i, length
                    il.Emit(OpCodes.Newarr, t_int); // t, t, c, f, f, i, output
                    for(int j = 0; j < output.Length; j++)
                    {
                        il.Emit(OpCodes.Dup); // t, t, c, f, f, i, o, o
                        // непосредственная загрузка элементов
                        il.Emit(OpCodes.Ldc_I4, j); // t, t, c, f, f, i, o, o, j
                        il.Emit(OpCodes.Ldc_I4, output[j]); // t, t, c, f, f, i, o, o, j, value
                        il.Emit(OpCodes.Stelem, t_int); // t, t, c, f, f, i, o
                    }
                    il.Emit(OpCodes.Stelem, t_array_int); // t, t, c, f
                }
                il.Emit(OpCodes.Call, method_transition_Add); // t
            }
            // теперь transition на стеке
        }

        public void EmitTransition(ILGenerator il, MethodBuilder m_de_dict)
        {
            var sb = new StringBuilder();
            SerializationCompiler.SerializeDict(transition, sb,(c, s) => s.Append((int)c) , SerializationCompiler.SerializeIntAA);
            SerializationCompiler.EmitSerializationLoading(il, sb.ToString()); // str
            il.Emit(OpCodes.Callvirt, SerializationCompiler.m_GetEnum); // se
            il.Emit(OpCodes.Call, m_de_dict); // trans
        }

        [Obsolete("Serialization version must be used")]
        public void EmitFinals(ILGenerator il)
        {
            // создание объекта
            il.Emit(OpCodes.Newobj, ctor_finals); // finals
            foreach(var e in finalStates)
            {
                il.Emit(OpCodes.Dup); // f, f
                il.Emit(OpCodes.Ldc_I4, e); // f, f, e
                il.Emit(OpCodes.Call, method_finals_Add); // f, add_result
                il.Emit(OpCodes.Pop); // f
            }
            // теперь finals на стеке
        }

        public void EmitFinals(ILGenerator il, MethodInfo m_de_set)
        {
            var sb = new StringBuilder();
            SerializationCompiler.SerializeArray(finalStates, sb);
            SerializationCompiler.EmitSerializationLoading(il, sb.ToString()); // str
            il.Emit(OpCodes.Callvirt, SerializationCompiler.m_GetEnum); // se
            il.Emit(OpCodes.Call, m_de_set); // finals
        }
    }
}
