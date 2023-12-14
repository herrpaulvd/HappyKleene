using System;
using System.Collections.Generic;
using System.Text;

// данный namespace задаёт систему классов, отвечающих за
// анализ и компиляцию кода, связанного с лексическим анализом
namespace HappyKleene.LexLanguage
{
    // альяс для списка кортежей-токенов
    // токен задаётся тремя параметрами
    // 1) собственно строкое представление
    // 2) тип токена, заданный перечислением Tokenizer.TokenKind
    // 3) некоторое число, связанное с этим токеном, например, степень, в которую
    // возводится указанное выражение
    using TokenList = List<(string, Tokenizer.TokenKind, int)>;

    static class LexParser
    {
        // Парсер описания лексического анализатора на языке HappyKleene-lex (HK-lex)
        // в теле описания
        // разрешены:
        // + строки
        // + конкатенация
        // + плюс Клини, звезда Клини, все виды степеней
        // + объединение, пересечение
        // + специальные идентификаторы
        // запрещены:
        // - идентификаторы

        // далее следуют вспомогательные функции разбора выражений языка HK-lex
        // все они принимают два параметра
        // 1) список токенов языка HK-lex
        // 2) позиция p (по ссылке), с которой начинается разбор выражения
        // при успешном завершении разбора p будет указывать на последнюю
        // непрочитанную позицию
        // в противном случае - начальное значение p
        // возвращается представление выражения в виде дерева
        // подробное описание назначений методов см. в методе Parse

        private static IExpression ReadAtom(TokenList tokens, ref int p)
        {
            int p1 = p; // сохраняем начальную позицию разбора

            // в качестве атома HK-lex мы рассматриваем
            switch (tokens[p].Item2)
            {
                // 1) специальный идентификатор
                case Tokenizer.TokenKind.tkSpecialId:
                    return SpecialIds.GetItem(tokens[p++].Item1);
                // строку CharSequence
                case Tokenizer.TokenKind.tkCharSequence:
                    return new CharSequence(Tokenizer.ExtractCharSequenceSource(tokens[p++].Item1));
                // выражение в скобках
                // его мы называем атомом только потому, что оно ведёт себя как типичный
                // атом языка, например, является непосредственным операндом последующих
                // постфиксных унарных операций
                case Tokenizer.TokenKind.tkOpenedBracket:
                    p++; // пропускаем скобку
                    var union = ReadUnion(tokens, ref p); // пытаемся прочитать выражение
                    if (!(union is object)) // если выражение не прочитано, откатываемся и возвращаем null
                    {
                        p = p1;
                        return null;
                    }
                    var tkEnd = tokens[p++].Item2;
                    if (tkEnd == Tokenizer.TokenKind.tkClosedBracket) // возвращаем атом, если встретилась
                        return union;                                 // закрывающая скобка
                    p = p1;
                    return null; // в противном случае - null с откатом
                default:
                    return null;
            }
        }

        private static IExpression ReadUnaryApp(TokenList tokens, ref int p)
        {
            int p1 = p; // сохраняем предыдущий результат для отката

            // наши унарные операции - постфиксные => сначала нужно прочитать выражение,
            // к которому они применяются
            // применяться они могут только к атому
            var result = ReadAtom(tokens, ref p);

            // при отсутствии объекта применения нет применения унарной операции
            if (!(result is object))
            {
                p = p1; // (напомню) откат
                return null;
            }

            // дальше мы считываем весь список следующих унарных операций
            while (p < tokens.Count)
            {
                // джокер указывает на то, что строковое представление
                // токена нас не интересует - вся информация для унарных операций
                // содержится в двух остальных полях
                (_, Tokenizer.TokenKind opkind, int n) = tokens[p];

                // в зависимости от типа токена строится соответствующее применение
                // этой операции
                switch (opkind)
                {
                    case Tokenizer.TokenKind.tkKleenePlus:
                        result = result.GetKleenePlus();
                        break;
                    case Tokenizer.TokenKind.tkKleeneStar:
                        result = result.GetKleeneStar();
                        break;
                    case Tokenizer.TokenKind.tkPlusPower:
                        result = result.GetPlusPower(n);
                        break;
                    case Tokenizer.TokenKind.tkStarPower:
                        result = result.GetStarPower(n);
                        break;
                    case Tokenizer.TokenKind.tkPower:
                        result = result.GetPower(n);
                        break;
                    default:
                        return result; // прекращаем разбор, когда не находим больше операций
                }

                p++;
            }

            return result;
        }

        private static IExpression ReadConcatenation(TokenList tokens, ref int p)
        {
            int p1 = p; // и снова сохранение для отката

            // читаем применение унарной операции или просто атом, если он есть, но
            // операций за ним нет
            var result = ReadUnaryApp(tokens, ref p);

            if (!(result is object))
            {
                p = p1;
                return null;
            }

            // конкатенация представлена несколькими применениями 
            // унарных операций
            while (p < tokens.Count)
            {
                var e = ReadUnaryApp(tokens, ref p);

                if (e is object)
                    result = result.ConcatWith(e);
                else
                    break;
            }

            return result;
        }

        // в принципе процедура с аналогичной логикой при учёте &
        private static IExpression ReadIntersection(TokenList tokens, ref int p)
        {
            int p1 = p;

            var result = ReadConcatenation(tokens, ref p);

            if (!(result is object))
            {
                p = p1;
                return null;
            }

            while (p < tokens.Count)
            {
                (_, Tokenizer.TokenKind kind, _) = tokens[p];
                if (kind != Tokenizer.TokenKind.tkIntersection)
                    break;

                if (++p == tokens.Count)
                {
                    p = p1;
                    return null;
                }

                var e = ReadConcatenation(tokens, ref p);

                if (!(e is object))
                {
                    p = p1;
                    return null;
                }

                result = result.IntersectWith(e);
            }

            return result;
        }

        // в принципе процедура с аналогичной логикой при учёте |
        private static IExpression ReadUnion(TokenList tokens, ref int p)
        {
            int p1 = p;

            var result = ReadIntersection(tokens, ref p);

            if (!(result is object))
            {
                p = p1;
                return null;
            }

            while (p < tokens.Count)
            {
                (_, Tokenizer.TokenKind kind, _) = tokens[p];
                if (kind != Tokenizer.TokenKind.tkUnion)
                    break;

                if (++p == tokens.Count)
                {
                    p = p1;
                    return null;
                }

                var e = ReadIntersection(tokens, ref p);

                if (!(e is object))
                {
                    p = p1;
                    return null;
                }

                result = result.UnionWith(e);
            }

            return result;
        }

        private static IExpression ReadExpression(TokenList tokens, ref int p)
        {
            int p1 = p;

            IExpression result = ReadUnion(tokens, ref p);
            if (!(result is object) || p == tokens.Count || tokens[p].Item2 != Tokenizer.TokenKind.tkRuleEnd)
            {
                p = p1;
                return null;
            }

            p++;
            return result;
        }

        // собственно метод получения правил лексического анализа
        // из описания на языке HK-lex
        public static List<Rule> Parse(string file)
        {
            const string EOF = "Unexpected end of file";

            var tokens = Tokenizer.GetTokens(file);

            int p = 0;
            List<Rule> result = new List<Rule>();

            // scheme : id = expression
            while (p < tokens.Count)
            {
                // процесс считывания правила:
                // 1) идентификатор
                (string idname, Tokenizer.TokenKind kind, _) = tokens[p];

                if (kind != Tokenizer.TokenKind.tkId)
                    throw new BuildingException("Identificator expected", result.Count + 1);

                if (++p == tokens.Count) throw new BuildingException(EOF, result.Count + 1);

                // 2) =
                (_, kind, _) = tokens[p];

                if (kind != Tokenizer.TokenKind.tkAssign)
                    throw new BuildingException("Assignation symbol expected", result.Count + 1);

                if (++p == tokens.Count) throw new BuildingException(EOF, result.Count + 1);

                // 3) разбор выражения справа
                // atom = specialId | charSequence | ( union )
                // unary-app = atom | unary-app unaryOp
                // concatenation = unary_app +
                // intersection = concatenation (<&> concatenation) *
                // union = intersection (<|> intersection) *
                // expression = union ;
                //
                // где expression - стартовый нетерминал
                // <|> обозначает символ |, <&> - символ &
                // промоделируем разбор этой несложной грамматики простым бэктрекингом
                // ReadExpression - читает нетерминал expression
                // ReadUnion - нетерминал union
                // и т.д.
                // нетрудно показать, что такой разбор данной грамматики выполняется за
                // лучшее время - O(N) (N - длина строки)
                // для доказательства можно рассмотреть дерево рекурсии с учётом различных
                // результатов (прочитано / не прочитано) и показать,
                // что количество вершин в нём - O(N)

                IExpression right = ReadExpression(tokens, ref p);

                if (right is object) // некорректно => null
                    result.Add(new Rule(idname, right));
                else
                    throw new BuildingException("Incorrect expression to the right of '=' sign", result.Count + 1);
            }

            return result;
        }
    }
}
