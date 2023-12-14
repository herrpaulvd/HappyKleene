using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HappyKleene
{
    // главный класс интепретации
    class Interpreter
    {
        // null, если интерпретация прошла без ошибок
        // строка-ошибка иначе
        // если возвращается null, то результат интепретации - дерево правого вывода -
        // записывается в файл с именем проекта + .log.txt
        public static string Run(string lexcode, string syncode, string txtfile, string projectName, string projectDirectory)
        {
            try
            {
                var lexrules = LexLanguage.LexParser.Parse(lexcode);
                var tokens = LexAnalyzer.Analyze(lexrules, txtfile);
                var grammar = SynLanguage.SynParser.Parse(lexrules.Select(r => r.Left), syncode);
                if (!(grammar is object)) throw new BuildingException("Empty language");
                var analysisSequence = new EarleyListSystem().GetRightAnalysis(grammar, tokens.Select(t => t.Item1), tokens.Select(t => t.Item3).Concat(new int[] { txtfile.Length }).ToArray());
                var tree = ParsingTree.Build(tokens.Select(t => t.Item2), grammar, analysisSequence);
                File.WriteAllText(projectDirectory + projectName + ".log.txt", tree.ToString());
            }
            catch(BuildingException ex)
            {
                return ex.Message;
            }
            catch(ExpectedEOFException ex)
            {
                return ex.Message;
            }
            catch(ExpectedTokensException ex)
            {
                return ex.Message;
            }
            catch(UnexpectedSymbolException ex)
            {
                return ex.Message;
            }
            return null;
        }
    }
}
