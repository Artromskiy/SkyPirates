using System;
using System.Collections.Generic;
using System.Text;

namespace DVG.Editor.CodeGen
{
    public static class StringExtensions
    {
        public static string Loop(this IEnumerable<string> args, Converter<string, string> converter)
        {
            StringBuilder code = new();
            foreach (var arg in args)
                if (!string.IsNullOrEmpty(arg))
                    code.Append(converter(arg));
            return code.ToString();
        }

        public static string Loop<T>(this IEnumerable<T> args, Converter<T, string> converter)
        {
            StringBuilder code = new();
            foreach (var arg in args)
                code.Append(converter(arg));
            return code.ToString();
        }
    }
}
