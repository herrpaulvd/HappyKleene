using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HappyKleene
{
    // вспомогательные методы расширения
    static class ExtensionMethods
    {
        // метод добавляет ко всей входной последовательности целых чисел
        // определённое число n
        public static IEnumerable<int> MapAdd(this IEnumerable<int> en, int n)
            => en.Select(x => x + n);
    }
}
