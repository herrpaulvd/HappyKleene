using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

// здесь происходит парсинг языка HappyKleene-syn (HK-syn)
// для разбора описания синтаксического анализа
namespace HappyKleene.SynLanguage
{
    // альяс для списка кортежей-токенов
    // токен задаётся тремя параметрами
    // 1) собственно строкое представление
    // 2) тип токена, заданный перечислением Tokenizer.TokenKind
    // 3) некоторое число, связанное с этим токеном, например, степень, в которую
    // возводится указанное выражение
    using TokensList = List<(string, Tokenizer.TokenKind, int)>;

    static class SynParser
    {
        // Парсер описания лексического анализатора
        // разрешены:
        // + конкатенация
        // + идентификаторы
        // + объединение верхнего уровня!!!
        // + \empty
        // запрещены:
        // - плюс Клини, звезда Клини, все виды степеней
        // - почти все специальные идентификаторы
        // - строки
        // - все остальные объединения

        // первое предложение должно быть таким:
        // = id ; // id - имя стартового нетерминала

        // грамматика выражений
        // atom = id | \empty [\empty будем обрабатывать в программе как ""]
        // concatenation = atom + [is List<string>]
        // expression = concatenation ( <|> concatenation ) * ; [is List<List<string>>]
        // эту грамматику тоже можно обработать бэктрекингом за O(N)

        // здесь логика: если id или \empty - вернуть, иначе null
        private static string ReadAtom(TokensList tokens, ref int p)
        {
            var tk = tokens[p];
            if (tk.Item2 == Tokenizer.TokenKind.tkId)
            {
                p++;
                return tk.Item1;
            }

            if (tk.Item2 == Tokenizer.TokenKind.tkSpecialId && tk.Item1 == "\\empty")
            {
                p++;
                return "\\empty";
            }

            return null;
        }

        // если есть хотя бы 1 атом - считываем его  и остальные,
        // когда атомы кончаются - возвращаемся (при полном отсутствии атомов возвращаем null)
        // здесь же будем пропускать атомы \empty, т.к. они обозначают только пустое тело
        private static List<string> ReadConcatenation(TokensList tokens, ref int p)
        {
            var atom = ReadAtom(tokens, ref p);
            if (!(atom is object)) return null;

            var result = new List<string> { atom };
            while (p < tokens.Count)
            {
                atom = ReadAtom(tokens, ref p);
                if (atom is object) result.Add(atom);
                else break;
            }

            return result.Where(s => s != "\\empty").ToList();
        }

        // аналогичная логика но с учётом разделителей |
        private static List<List<string>> ReadExpression(TokensList tokens, ref int p)
        {
            int p1 = p;

            var conc = ReadConcatenation(tokens, ref p);
            if (!(conc is object)) return null;

            var result = new List<List<string>>() { conc };

            while (p < tokens.Count)
            {
                var tk = tokens[p];
                if (tk.Item2 != Tokenizer.TokenKind.tkUnion)
                    break;

                p++;
                if (p == tokens.Count)
                {
                    p = p1;
                    return null;
                }
                conc = ReadConcatenation(tokens, ref p);
                if (conc is object)
                    result.Add(conc);
                else
                {
                    p = p1;
                    return null;
                }
            }

            var tkEnd = tokens[p];
            if (tkEnd.Item2 == Tokenizer.TokenKind.tkRuleEnd)
            {
                p++;
                return result;
            }

            p = p1;
            return null;
        }

        public static Grammar Parse(IEnumerable<string> terminals, string synfile)
        {
            const string EOF = "Unexpected end of file";

            var tokens = Tokenizer.GetTokens(synfile);

            // первое и главное предложение: = StartItem ;
            if (tokens.Count < 3)
                throw new BuildingException(EOF);

            if (tokens[0].Item2 != Tokenizer.TokenKind.tkAssign)
                throw new BuildingException("First expression: '=' expected");

            if (tokens[1].Item2 != Tokenizer.TokenKind.tkId)
                throw new BuildingException("First expression: identificator expected");

            if (tokens[2].Item2 != Tokenizer.TokenKind.tkRuleEnd)
                throw new BuildingException("First expression: ';' expected");

            // создаём грамматику с указанным стартом
            Grammar g = new Grammar(tokens[1].Item1);
            foreach (var t in terminals) g.AddTerminal(t);
            bool hasStart = false; // встречается ли стартовый нетерминал вообще в левых частях

            List<(string, List<string>)> rules = new List<(string, List<string>)>();

            int p = 3; // skip =, id, ;.

            int ruleID = 0;

            while (p < tokens.Count)
            {
                ruleID++;

                // 1) id в левой части
                (string idname, Tokenizer.TokenKind kind, _) = tokens[p];

                if (kind != Tokenizer.TokenKind.tkId)
                    throw new BuildingException("Identificator expected", ruleID);

                if (idname == tokens[1].Item1)
                    hasStart = true;

                if (++p == tokens.Count) throw new BuildingException(EOF, ruleID);

                // 2) =
                (_, kind, _) = tokens[p];

                if (kind != Tokenizer.TokenKind.tkAssign)
                    throw new BuildingException("Assignation symbol expected", ruleID);

                if (++p == tokens.Count) throw new BuildingException(EOF, ruleID);

                // 3) expression
                var expr = ReadExpression(tokens, ref p);

                if (expr is object)
                    rules.AddRange(expr.Select(r => (idname, r)));
                else
                    throw new BuildingException("Incorrect expression to the right of '=' sign", ruleID);
            }

            if (!hasStart)
                throw new BuildingException("Start non-terminal doesn't have any rule", ruleID);

            foreach ((string id, _) in rules)
                g.AddNonTerminal(id);

            ruleID = 0;
            foreach ((string id, List<string> production) in rules)
                g.AddRule(id, production, ruleID++);

            return g.MakeBetter();
        }
    }
}
