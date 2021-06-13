using System;

namespace JsonDeepEqualAssertions
{
    public static class Color
    {
        public static string Red(string input)
        {
            return Console.IsErrorRedirected ? $"\u001b[31;1m{input}\u001b[m" : input;
        }
    }
}