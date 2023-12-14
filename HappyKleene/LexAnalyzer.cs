using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace HappyKleene
{
    // [интерпретатор] Лексический анализ
    static class LexAnalyzer
    {
        // стратегия лексического анализа
        // ведём параллельный запуск автоматов, соответствующих разным типам лексем
        // выбираем наидлиннейший принятый автоматом префикс
        // и тип этой лексемы будет типом первого по приоритету принявшего её автомата
        public static List<(string, string, int)> Analyze(List<LexLanguage.Rule> rules, string file)
        {
            var machines = rules.Select(r => r.BuildFSM()).ToArray();

            int end = 0;

            List<(string, string, int)> result = new List<(string, string, int)>();
            Queue<int> autoq = new Queue<int>(); // очередь автоматов, которые ещё могут продолжать работу

            while (end < file.Length)
            {
                int lastAccepted = -1; // последний принятый автоматом символ
                                       // лексему образует подстрока, начинающаяся с end и заканчивающаяся lastAccepted
                int acceptorId = -1; // принявший символ #lastAccepted автомат

                autoq.Clear();
                for (int i = 0; i < machines.Length; i++)
                {
                    autoq.Enqueue(i);
                    machines[i].Reset();
                }
                autoq.Enqueue(-1); // -1 обозначает конец текущей итерации автоматов

                for (int i = end; i < file.Length && autoq.Count > 1; i++)
                {
                    while (autoq.Peek() != -1)
                    {
                        int machineId = autoq.Dequeue();
                        if (machines[machineId].Tact(file[i]))
                        {
                            autoq.Enqueue(machineId);
                            if (i > lastAccepted && machines[machineId].Final())
                            {
                                lastAccepted = i;
                                acceptorId = machineId;
                            }
                        }
                    }
                    autoq.Dequeue();
                    autoq.Enqueue(-1);
                }

                // если лексема не найдена
                if (lastAccepted == -1)
                    throw new UnexpectedSymbolException(end);

                // если прочитанная лексема имеет тип "джокер", мы её просто не включаем в отчёт об анализе
                if (rules[acceptorId].Left != "_")
                    result.Add((rules[acceptorId].Left, file.Substring(end, lastAccepted + 1 - end), end));
                end = lastAccepted + 1;
            }

            return result;
        }
    }
}
