using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Text.Json;

namespace JsonDeepEqualAssertions
{
    internal class JsonDeepEqualContext
    {
        public JsonDeepEqualContext(JsonDeepEqualOptions options)
        {
            Options = options;
        }

        public JsonPath ExpectedPath { get; private set; } = new JsonPath();
        public JsonPath ActualPath { get; private set; } = new JsonPath();
        public JsonDeepEqualOptions Options { get; }

        public JsonDeepEqualContext WithAddedPart(string part)
        {
            return WithAddedPart(part, part);
        }

        public JsonDeepEqualContext WithAddedPart(string expectedPart, string actualPart)
        {
            var n = new JsonDeepEqualContext(Options);
            n.ExpectedPath = n.ExpectedPath.WithAddedPart(expectedPart);
            n.ActualPath = n.ExpectedPath.WithAddedPart(actualPart);
            return n;
        }
    }

    public static class JsonDeepEqual
    {
        private static IEnumerable<JsonDifference> FindSetDifferences(JsonElement expected, JsonElement actual,
            JsonDeepEqualContext ctx)
        {
            var (exp, act) = (expected.EnumerateArray().ToList(), actual.EnumerateArray().ToList());

            var matchingPairs =
                exp.SelectMany((expElem, i) =>
                        act.Select((actElem, j) =>
                            (i, j, FindDifferences(expElem, actElem, ctx.WithAddedPart($"[{i}]", $"[{j}]"))))
                    )
                    .Where(t => !t.Item3.Any())
                    .Select(t => (ExpectedIndex: t.i, ActualIndex: t.j));

            var expectedIndices = Enumerable.Range(0, exp.Count).ToHashSet();
            var actualIndices = Enumerable.Range(0, act.Count).ToHashSet();

            foreach (var (i, j) in matchingPairs.Where(t =>
                expectedIndices.Contains(t.ExpectedIndex) && actualIndices.Contains(t.ActualIndex)))
            {
                expectedIndices.Remove(i);
                actualIndices.Remove(j);
            }

            foreach (var i in expectedIndices.OrderBy(x => x))
            {
                yield return new JsonDifference(
                    JsonTarget.Expected,
                    ctx.ExpectedPath.WithAddedPart($"[{i}]"),
                    $"expected contains the following element that was not in actual:\n{JsonPrinter.PrintElement(exp[i])}"
                );
            }

            foreach (var j in actualIndices.OrderBy(x => x))
            {
                yield return new JsonDifference(
                    JsonTarget.Actual,
                    ctx.ExpectedPath.WithAddedPart($"[{j}]"),
                    $"actual contains the following element that was not in expected:\n{JsonPrinter.PrintElement(act[j])}"
                );
            }
        }

        private static IEnumerable<JsonDifference> FindArrayDifferences(JsonElement expected, JsonElement actual,
            JsonDeepEqualContext ctx)
        {
            if (ctx.Options.IgnoreArrayOrdering)
            {
                foreach (var diff in FindSetDifferences(expected, actual, ctx))
                {
                    yield return diff;
                }

                yield break;
            }

            var (exp, act) = (expected.EnumerateArray().ToList(), actual.EnumerateArray().ToList());

            for (var i = 0; i < Math.Min(exp.Count, act.Count); ++i)
            {
                foreach (var difference in FindDifferences(exp[i], act[i], ctx.WithAddedPart($"[{i}]")))
                {
                    yield return difference;
                }
            }

            foreach (var (i, e) in exp.Enumerate().Skip(Math.Min(exp.Count, act.Count)))
            {
                yield return new JsonDifference(
                    JsonTarget.Expected,
                    ctx.ExpectedPath.WithAddedPart($"[{i}]"),
                    $"the following element is present in expected, but is not present in actual\n{JsonPrinter.PrintElement(e)}"
                );
            }

            foreach (var (i, e) in act.Enumerate().Skip(Math.Min(exp.Count, act.Count)))
            {
                yield return new JsonDifference(
                    JsonTarget.Actual,
                    ctx.ActualPath.WithAddedPart($"[{i}]"),
                    $"the following element is present in actual, but is not present in expected\n{JsonPrinter.PrintElement(e)}"
                );
            }
        }

        private static IEnumerable<JsonDifference> FindObjectDifferences(JsonElement expected, JsonElement actual,
            JsonDeepEqualContext ctx)
        {
            var exp = expected.EnumerateObject().ToDictionary(x => x.Name, x => x.Value);
            var act = actual.EnumerateObject().ToDictionary(x => x.Name, x => x.Value);

            foreach (var key in exp.Keys.OrderBy(x => x))
            {
                if (!act.ContainsKey(key))
                {
                    yield return new JsonDifference(
                        JsonTarget.Expected,
                        ctx.ExpectedPath.WithAddedPart($".{key}"),
                        $"expected contains key {JsonPrinter.StringToJsonValue(key)} that is not present in actual"
                    );
                    continue;
                }

                foreach (var difference in FindDifferences(exp[key], act[key], ctx.WithAddedPart($".{key}")))
                {
                    yield return difference;
                }
            }

            foreach (var key in act.Keys.Except(exp.Keys).OrderBy(x => x))
            {
                yield return new JsonDifference(
                    JsonTarget.Actual,
                    ctx.ActualPath.WithAddedPart($".{key}"),
                    $"actual contains key {JsonPrinter.StringToJsonValue(key)} that is not present in expected"
                );
            }
        }

        private static IEnumerable<JsonDifference> FindNumberDifferences(JsonElement expected, JsonElement actual,
            JsonDeepEqualContext ctx)
        {
            var (e, a) = (expected.GetDouble(), actual.GetDouble());

            if (Math.Abs(e - a) > 0.000001)
            {
                yield return new JsonDifference(
                    JsonTarget.Expected,
                    ctx.ExpectedPath,
                    $"{expected} is not equal to {actual}");
            }
        }

        private static IEnumerable<JsonDifference> FindStringDifferences(JsonElement expected, JsonElement actual,
            JsonDeepEqualContext ctx)
        {
            var (e, a) = (expected.GetString(), actual.GetString());

            if (e != a)
            {
                yield return new JsonDifference(
                    JsonTarget.Expected,
                    ctx.ExpectedPath,
                    $"{JsonPrinter.StringToJsonValue(e)} is not equal to {JsonPrinter.StringToJsonValue(a)}");
            }
        }

        private static IEnumerable<JsonDifference> FindDifferences(JsonElement expected, JsonElement actual,
            JsonDeepEqualContext ctx)
        {
            if (expected.ValueKind != actual.ValueKind)
            {
                yield return new JsonDifference(
                    JsonTarget.Expected,
                    ctx.ExpectedPath,
                    $"expected {expected.ValueKind}, but got {actual.ValueKind}"
                );
                yield break;
            }

            Func<JsonElement, JsonElement, JsonDeepEqualContext, IEnumerable<JsonDifference>> f =
                expected.ValueKind switch
                {
                    JsonValueKind.Array => FindArrayDifferences,
                    JsonValueKind.Object => FindObjectDifferences,
                    JsonValueKind.Number => FindNumberDifferences,
                    JsonValueKind.String => FindStringDifferences,
                    _ => (x, y, z) => Array.Empty<JsonDifference>()
                };

            foreach (var error in f(expected, actual, ctx))
            {
                yield return error;
            }
        }

        private static JsonElement ParseJson(string json)
        {
            return JsonDocument.Parse(json).RootElement;
        }

        private static JsonDifference InvalidJsonDifference(JsonTarget target, string json,
            JsonException parseException)
        {
            var line = json.Split("\n")[parseException.LineNumber ?? 0];
            var pos = new Index((int) (parseException.BytePositionInLine ?? 0));
            var before = line[..pos]!;
            var after = line[pos..]!;

            return new JsonDifference(
                target,
                new JsonPath(),
                $"Failed to parse {target.ToString().ToLower()} as JSON: {parseException.Message}\n\n> {before}{Color.Red(after)}");
        }

        private static (JsonElement? Expected, JsonElement? Actual, IList<JsonDifference> ParseErrors)
            ParseExpectedAndActualJson(string expectedJson, string actualJson)
        {
            // this would be nicer in F#

            JsonElement? expected = null;
            JsonElement? actual = null;
            var errors = new List<JsonDifference>();

            try
            {
                expected = ParseJson(expectedJson);
            }
            catch (JsonException e)
            {
                errors.Add(InvalidJsonDifference(JsonTarget.Expected, expectedJson, e));
            }

            try
            {
                actual = ParseJson(actualJson);
            }
            catch (JsonException e)
            {
                errors.Add(InvalidJsonDifference(JsonTarget.Actual, expectedJson, e));
            }

            return (expected, actual, errors);
        }

        /// <summary>
        /// Finds the differences between two JSON strings.
        /// </summary>
        public static IEnumerable<JsonDifference> FindDifferences(string expectedJson, string actualJson,
            JsonDeepEqualOptions options)
        {
            var (expected, actual, errors) = ParseExpectedAndActualJson(expectedJson, actualJson);

            if (errors.Any())
            {
                return errors;
            }

            return FindDifferences(expected!.Value, actual!.Value, new JsonDeepEqualContext(options));
        }

        /// <summary>
        /// Finds the differences between two JSON strings.
        /// </summary>
        public static IEnumerable<JsonDifference> FindDifferences(string expectedJson, string actualJson)
        {
            return FindDifferences(expectedJson, actualJson, new JsonDeepEqualOptions());
        }
       
        /// <summary>
        /// Asserts that two JSON strings are equal, calling ifNotEqual with a descriptive error message if they aren't.
        /// </summary>
        public static void AssertEqual(string expectedJson, string actualJson, Action<string> ifNotEqual, JsonDeepEqualOptions options)
        {
            var (expected, actual, errors) = ParseExpectedAndActualJson(expectedJson, actualJson);

            if (errors.Any())
            {
                ifNotEqual($@"
one or more arguments were not valid JSON:
{string.Join("\n", errors)}
".Trim());
                return;
            }

            var differences = FindDifferences(expectedJson, actualJson, options).ToList();
            if (!differences.Any())
            {
                return;
            }

            var expPaths = differences
                .Where(x => x.Target == JsonTarget.Expected)
                .Select(x => x.Path);
            var actPaths = differences
                .Where(x => x.Target == JsonTarget.Actual)
                .Select(x => x.Path);

            ifNotEqual($@"
the expected JSON was not equal to the actual JSON:
{string.Join("\n", differences.Select(x => x.ToString()))}

expected:
{JsonPrinter.PrintElement(expected!.Value, expPaths)}

actual:
{JsonPrinter.PrintElement(actual!.Value, actPaths)}
".Trim());
        }

        /// <summary>
        /// Asserts that two JSON strings are equal, calling ifNotEqual with a descriptive error message if they aren't.
        /// </summary>
        public static void AssertEqual(string expectedJson, string actualJson, Action<string> ifNotEqual)
        {
            AssertEqual(expectedJson, actualJson, ifNotEqual, new JsonDeepEqualOptions());
        }
    }
}