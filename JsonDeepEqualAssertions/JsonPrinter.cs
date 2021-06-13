using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace JsonDeepEqualAssertions
{
    internal static class JsonPrinter
    {
        public static string StringToJsonValue(string s)
        {
            return JsonSerializer.Serialize(s);
        }

        private static string GetIndent(int indentationLevel)
        {
            var sb = new StringBuilder();
            foreach (var _ in Enumerable.Range(0, indentationLevel))
            {
                sb.Append("  ");
            }

            return sb.ToString();
        }

        private static string PrintObject(JsonElement e, ISet<JsonPath> pathsToHighlight, int indentationLevel,
            JsonPath currentPath)
        {
            var sb = new StringBuilder();
            sb.Append(GetIndent(indentationLevel));
            sb.Append("{\n");

            foreach (var (key, value) in e.EnumerateObject().OrderBy(x => x.Name).Select(x => (x.Name, x.Value)))
            {
                var targetPath = currentPath.WithAddedPart($".{key}");

                sb.Append(GetIndent(indentationLevel + 1));
                sb.Append(pathsToHighlight.Contains(targetPath)
                    ? Color.Red(StringToJsonValue(key))
                    : StringToJsonValue(key));
                sb.Append(": ");

                sb.Append(PrintElement(value, pathsToHighlight, indentationLevel + 1, targetPath));
                sb.Append(",\n");
            }

            sb.Remove(sb.Length - 2, 2);
            sb.Append("\n");
            sb.Append(GetIndent(indentationLevel));
            sb.Append("}");

            return sb.ToString();
        }

        private static string PrintArray(JsonElement e, ISet<JsonPath> pathsToHighlight, int indentationLevel,
            JsonPath currentPath)
        {
            var sb = new StringBuilder();
            sb.Append("[\n");

            foreach (var (i, value) in e.EnumerateArray().Enumerate())
            {
                var targetPath = currentPath.WithAddedPart($"[{i}]");
                var elString = PrintElement(value, pathsToHighlight, indentationLevel + 1, targetPath);

                sb.Append(GetIndent(indentationLevel + 1));
                sb.Append(pathsToHighlight.Contains(targetPath)
                    ? Color.Red(elString)
                    : elString);
                sb.Append(",\n");
            }

            sb.Remove(sb.Length - 2, 2);
            sb.Append("\n");
            sb.Append(GetIndent(indentationLevel));
            sb.Append("]");

            return sb.ToString();
        }

        private static string PrintValue(JsonElement e, ISet<JsonPath> pathsToHighlight, int indentationLevel,
            JsonPath currentPath)
        {
            var s = e.ToString();
            if (pathsToHighlight.Contains(currentPath))
            {
                s = Color.Red(s);
            }

            return s;
        }
        
        private static string PrintElement(JsonElement e, ISet<JsonPath> pathsToHighlight, int indentationLevel,
            JsonPath currentPath)
        {
            Func<JsonElement, ISet<JsonPath>, int, JsonPath, string> printer = e.ValueKind switch
            {
                JsonValueKind.Array => PrintArray,
                JsonValueKind.Object => PrintObject,
                _ => PrintValue
            };

            return printer(e, pathsToHighlight, indentationLevel, currentPath);
        }

        public static string PrintElement(JsonElement e, IEnumerable<JsonPath> pathsToHighlight)
        {
            return PrintElement(e, pathsToHighlight.ToHashSet(), 0, new JsonPath());
        }

        public static string PrintElement(JsonElement e)
        {
            return PrintElement(e, Array.Empty<JsonPath>());
        }
    }
}