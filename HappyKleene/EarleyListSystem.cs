using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace HappyKleene
{
    // моё видение реализации алгоритма Эрли
    class EarleyListSystem
    {
        // в кортежах договариваемся хранить ситуации в формате
        // (ruleId, pointLocation, second)

        // класс списка в алгоритме Эрли
        private class EarleyList
        {
            private EarleyListSystem system; // родительская система
            private int index; // индекс списка

            public EarleyList(EarleyListSystem system, int index)
            {
                this.system = system;
                this.index = index;
            }

            // все ситуации 
            public List<(int, int, int)> situations = new List<(int, int, int)>();
            public SortedSet<(int, int, int)> situationsSet = new SortedSet<(int, int, int)>();
            // элемент history это
            // (ruleId, pointLocation, second, A, i)
            // где первая тройка - ситуация
            // (endRuleID, i) - конечная ситуация, которая привела к возникновению ситуации
            // или (-1, -1) если таковой не существует
            public List<(int, int, int, int, int)> history = new List<(int, int, int, int, int)>();

            // конечные ситуации ([A -> ...#, i]). Храним как функцию (A, i) -> ruleId
            // запросы:
            // 1) добавление
            // 2) проверка существования
            private SortedDictionary<(int, int), int> endSituations = new SortedDictionary<(int, int), int>();

            private void AddEndSituation(int A, int i, int endRuleId)
            {
                if (endSituations.ContainsKey((A, i))) return;
                endSituations.Add((A, i), endRuleId);

                // здесь работают пункты (2) и (5) (нетрудно показать, что (2) является частным случаем (5),
                // когда i = j = k = 0)
                // Эта ситуация принадлежит списку #index и имеет второй компонентой i.
                // Тогда ищем в списке #i middle situations, т.е. ситуации типа [B -> ...#A..., second]
                // и включаем их с перемещением головки # на 1 символ (ибо конечная ситуация подразумевает,
                // что символ прочитан)
                foreach (var (ruleId, pointLocation, second) in system[i].GetMiddleSituations(A))
                    Add(ruleId, pointLocation + 1, second, endRuleId, i);
            }

            private bool ExistsEndSituation(int A, int i)
                => endSituations.ContainsKey((A, i));

            // промежуточные ситуации
            // Храним просто их список по номеру следующего за точкой нетерминала
            private SortedDictionary<int, List<(int, int, int)>> middleSituations = new SortedDictionary<int, List<(int, int, int)>>();

            private void AddMiddleSituation(int ruleId, int pointLocation, int second)
            {
                int A = system.grammar[ruleId].Item2[pointLocation];
                if (system.grammar.IsTerminal(A)) return;
                if (!middleSituations.ContainsKey(A))
                    middleSituations.Add(A, new List<(int, int, int)>());
                middleSituations[A].Add((ruleId, pointLocation, second));

                // здесь реализуем пункты алгоритма (2) и (5)
                if (ExistsEndSituation(A, second))
                    Add(ruleId, pointLocation + 1, second, endSituations[(A, second)], second);

                // здесь пункты (3) и (6)
                AddAllStartSituations(A);
            }

            private static readonly (int, int, int)[] emptyarr = new (int, int, int)[0];
            private IEnumerable<(int, int, int)> GetMiddleSituations(int A)
            {
                if (middleSituations.ContainsKey(A))
                    return middleSituations[A];
                return emptyarr;
            }

            // стартовые ситуации
            // эта подструктура нужна для того, чтобы на шаге 6 не выполнялось
            // добавление одной стартовой ситуации много раз сразу
            // так мы поддерживаем уникальность элементов в списке situations
            // и всех списках middleSituations

            private SortedSet<int> startSources = new SortedSet<int>();

            // добавляет ситуации по пунктам (3) и (6) (3 - частный случай (6))
            // здесь при middle ситуации если за головкой находится нетерминал,
            // то следует добавить все его стартовые ситуации
            private void AddAllStartSituations(int src)
            {
                if (startSources.Contains(src)) return;
                startSources.Add(src);

                foreach (var rId in system.grammar.RuleIdsBySource(src))
                    Add(rId, 0, index, -1, -1);
            }

            // общее добавление
            // добавляет ситуацию
            // смотрит на её тип (middle или end)
            // и исходя из него вызывает необходимый обработчик
            private void Add(int ruleId, int pointLocation, int second, int hRuleId, int hi)
            {
                if (situationsSet.Contains((ruleId, pointLocation, second)))
                    return;

                situationsSet.Add((ruleId, pointLocation, second));
                situations.Add((ruleId, pointLocation, second));
                history.Add((ruleId, pointLocation, second, hRuleId, hi));

                if (system.grammar[ruleId].Item2.Length == pointLocation)
                    AddEndSituation(system.grammar[ruleId].Item1, second, ruleId);
                else
                    AddMiddleSituation(ruleId, pointLocation, second);
            }

            // методы построения списка
            // их всего два
            // построение как I0
            // чья суть - добавление всех правил из grammar.Start
            // посредством вызова Add (1 пункт алгоритма; 2, 3 вып. автоматически)
            // построение как Ij (j > 0)
            // просмотр списка I[j-1]
            // добавление необходимых средних правил (по п.4)
            // п.5 и 6 выполнятся автоматически

            public void BuildI0()
                => AddAllStartSituations(system.grammar.Start);

            public void BuildIj()
            {
                foreach (var (ruleId, pointLocation, second) in system[index - 1].situations)
                {
                    var rulebody = system.grammar[ruleId].Item2;
                    if (rulebody.Length == pointLocation) continue;
                    if (system.a[index - 1] == rulebody[pointLocation])
                        Add(ruleId, pointLocation + 1, second, -1, -1);
                }
            }

            // анализ таблицы списков
            // получение истории по ситуации
            public (int, int) GetHistory(int ruleId, int pointLocation, int second)
            {
                int id = situations.BinarySearch((ruleId, pointLocation, second));
                return (history[id].Item4, history[id].Item5);
            }

            // подготовка к анализу
            public void PrepareToAnalyze()
            {
                situations.Sort(); // чтобы бинпоиском искать прозвольные ситуации
                history.Sort();
            }
        }

        private EarleyList[] lists;
        private int[] a;
        private int[] truepositions; // длина +1 - длина исходного файла
        private Grammar grammar;
        private EarleyList this[int i] => lists[i];

        private void BuildLists()
        {
            lists = new EarleyList[a.Length + 1];
            for (int i = 0; i <= a.Length; i++) lists[i] = new EarleyList(this, i);
            lists[0].BuildI0();
            for (int i = 1; i <= a.Length; i++) lists[i].BuildIj();
        }

        // метод генерации ошибки анализа
        private Exception MakeError()
        {
            // назовём ситуацию [A -> @#a$] from I[j] проблемной, если
            // 1) a - терминал
            // 2) в I[j + 1] нет ситуации [A -> @a#$]
            // следующий алгоритм находит самые "оптимистичные" проблемные ситуации
            // либо ситуации ожидания конца файла
            // ситуации о. к. ф. - это ситуации вида
            // [S -> @#, 0]
            
            // проблемные терминалы
            SortedSet<int> problems = new SortedSet<int>();
            // есть ожидание
            bool eofexpected = false;

            for(int j = a.Length; j >= 0; j--)
            {
                foreach(var (ruleId, pointLocation, second) in lists[j].situations)
                {
                    // проблемная ситуация?
                    if (pointLocation < grammar[ruleId].Item2.Length && grammar.IsTerminal(grammar[ruleId].Item2[pointLocation]))
                        problems.Add(grammar[ruleId].Item2[pointLocation]);
                    // ожидание?
                    if (grammar[ruleId].Item1 == grammar.Start && pointLocation == grammar[ruleId].Item2.Length && second == 0)
                        eofexpected = true;
                }

                // если есть информация о проблемной ситуации, то она
                // более правдоподобна, чем ситуация ожидания конца файла
                if (problems.Count > 0)
                    return new ExpectedTokensException(problems.Select(p => grammar.GetName(p)).ToArray(), truepositions[j]);
                if (eofexpected)
                    return new ExpectedEOFException(truepositions[j]);
            }

            throw new Exception("Internal error in MakeError() method");
        }

        // собственно анализ при построенных списках Эрли
        private List<int> Analyze()
        {
            // поиск конечной ситуации вида [S -> ...#, 0]
            // её существование означает, что строка синтаксически корректна (головка
            // успешно прочитала правило)
            int startRuleId = -1;
            foreach (var (ruleId, pointLocation, second) in lists[a.Length].situations)
                if (grammar[ruleId].Item1 == grammar.Start
                    && second == 0
                    && grammar[ruleId].Item2.Length == pointLocation)
                {
                    startRuleId = ruleId;
                    break;
                }

            if (startRuleId == -1) throw MakeError(); // если искомая успешная ситуация не была найдена
            List<int> result = new List<int>();
            foreach (var item in lists) item.PrepareToAnalyze();
            //собственно процедура R (изменённая согласно упр. 4.2.22 и 4.2.23)
            void R(int rule, int i, int j)
            {
                result.Add(rule);
                result.Add(truepositions[i]);

                var rulebody = grammar[rule].Item2;
                for (int k = rulebody.Length - 1; k >= 0; k--)
                    if (grammar.IsTerminal(rulebody[k]))
                    {
                        j--;
                        // добавление терминала в список
                        result.Add(-1);
                        result.Add(truepositions[j]);
                    }
                    else
                    {
                        var (endRuleId, r) = lists[j].GetHistory(rule, k + 1, i);
                        R(endRuleId, r, j);
                        j = r;
                    }
            }

            R(startRuleId, 0, a.Length);
            return result;
        }

        // полное построение правого анализа по списку токенов
        // здесь мы инициализируем некоторые начальные поля системы списков Эрли,
        // затем строим сами списки и возвращаем перевёрнутый правый разбор по ним
        public List<int> GetRightAnalysis(Grammar g, IEnumerable<string> tokens, int[] truepositions)
        {
            grammar = g;
            a = tokens.Select(t => g.GetId(t)).ToArray();
            this.truepositions = truepositions;
            BuildLists();
            return Analyze();
        }
    }
}
