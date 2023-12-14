using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HappyKleene.LexLanguage
{
    // методы для обработки специальных идентификаторов,
    // которые по сути представляют собой предопределённые константы
    // \t (табуляция), \n (\n и \r), \_ (собственно нижнее подчёркивание), 
    // \eng-letter (английские буквы), \digit (цифры), \empty (пустая строка), \except (с модификаторами)
    static class SpecialIds
    {
        private static char[] newline = { '\n', '\r' };
        private static char[] engLetters = 
            Enumerable.Range('a', 26).Concat(Enumerable.Range('A', 26)).Select(i => (char)i).ToArray();
        private static char[] digits = Enumerable.Range('0', 10).Select(i => (char)i).ToArray();
        private static char[] all = Enumerable.Range(0, char.MaxValue + 1).Select(i => (char)i).ToArray();

        private static IExpression ParseExcept(string tail)
        {
            SortedSet<char> denied = new SortedSet<char>();

            if (tail.Length % 2 > 0)
                return null;

            for(int i = 0; i < tail.Length; i++)
            {
                if (tail[i] == '$')
                {
                    i++;
                    if (tail[i] == '_') denied.Add(' ');
                    else denied.Add(tail[i]);
                }
                else if(tail[i] == '\\')
                {
                    i++;
                    switch(tail[i])
                    {
                        case 'n':
                            denied.UnionWith(newline);
                            break;
                        case 'd':
                            denied.UnionWith(digits);
                            break;
                        case 'e':
                            denied.UnionWith(engLetters);
                            break;
                        case 't':
                            denied.Add('\t');
                            break;
                        case '_':
                            denied.Add('_');
                            break;
                        default:
                            return null;
                    }
                }
            }

            return new CharSet(all.Where(c => !denied.Contains(c)));
        }

        // получение значение константы по названию
        public static IExpression GetItem(string id)
        {
            switch (id)
            {
                case "\\t":
                    return new CharSequence("\t");
                case "\\n":
                    return new CharSet(newline);
                case "\\_":
                    return new CharSequence("_");
                case "\\eng-letter":
                    return new CharSet(engLetters);
                case "\\digit":
                    return new CharSet(digits);
                case "\\empty":
                    return new CharSequence(string.Empty);
                case "\\all":
                    return new CharSet(all);
                default:
                    if (id.StartsWith("\\allexcept"))
                        return ParseExcept(id.Substring("\\allexcept".Length));
                    return null;
            }
        }
    }
}
