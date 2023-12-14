using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace HappyKleene
{
    // набор методов для разбиения HK-lex и HK-syn
    static class Tokenizer
    {
        public enum TokenKind // перечисление типов токенов
        {
            tkCharSequence,
            tkId,
            tkKleenePlus,
            tkKleeneStar,
            tkPlusPower,
            tkStarPower,
            tkPower,
            tkSpecialId,
            tkAssign,
            tkRuleEnd,
            tkOpenedBracket,
            tkClosedBracket,
            tkUnion,
            tkIntersection
        }

        // конвертирует строку в токен
        private static (string, TokenKind, int) GetToken(string item)
        {
            // одиночные
            if (item == "+") return ("+", TokenKind.tkKleenePlus, 0);
            if (item == "*") return ("*", TokenKind.tkKleeneStar, 0);
            if (item == "=") return ("=", TokenKind.tkAssign, 0);
            if (item == ";") return (";", TokenKind.tkRuleEnd, 0);
            if (item == "(") return ("(", TokenKind.tkOpenedBracket, 0);
            if (item == ")") return (")", TokenKind.tkClosedBracket, 0);
            if (item == "|") return ("|", TokenKind.tkUnion, 0);
            if (item == "&") return ("&", TokenKind.tkIntersection, 0);

            // типы
            int n;

            if (item[0] == '$') return (item, TokenKind.tkCharSequence, 0);
            if (item[0] == '+' && int.TryParse(item.Substring(1), out n) && n > 0)
                return (item, TokenKind.tkPlusPower, n);
            if (item[0] == '*' && int.TryParse(item.Substring(1), out n) && n > 0)
                return (item, TokenKind.tkStarPower, n);
            if (int.TryParse(item, out n) && n > 0)
                return (item, TokenKind.tkPower, n);
            if (item[0] == '\\' && item.Length > 1)
                return (item, TokenKind.tkSpecialId, 0);

            // всё остальное автоматически считается идентификаторами
            return (item, TokenKind.tkId, 0);
        }

        // разделителями являются пробел, табуляция и перенос строки
        private static readonly char[] separators = { ' ', '\n', '\t', '\r' };

        // разбивает на токены
        // комментариями будут скобки [ ] , естественно отделённые пробелами
        public static List<(string, TokenKind, int)> GetTokens(string file)
        {
            var result = new List<(string, TokenKind, int)>();

            bool comment = false;
            foreach (var s in file.Split(separators).Where(x => x.Length > 0))
            {
                if (comment)
                {
                    if (s == "]") comment = false;
                }
                else
                {
                    if (s == "[") comment = true;
                    else result.Add(GetToken(s));
                }
            }

            return result;
        }

        // преобразует представление константной строки в обычную форму
        // (заменяет _ на пробелы)
        public static string ExtractCharSequenceSource(string cs)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var c in cs.Skip(1))
                if (c == '_')
                    sb.Append(' ');
                else
                    sb.Append(c);

            return sb.ToString();
        }
    }
}
