using System;
using System.Linq;
using NUnit.Framework;

namespace JsonDeepEqualAssertions.Tests
{
    public class Tests
    {
        [TestCase(
            "{}",
            "{}"
        )]
        [TestCase(
            "{\"a\": 4, \"b\": 5}",
            "{\"a\": 4, \"b\": 5}"
        )]
        [TestCase(
            "{\"b\": 5, \"a\": 4}",
            "{\"a\": 4, \"b\": 5}"
        )]
        [TestCase(
            "[1, 2, 3]",
            "[3, 2, 1]"
        )]
        [TestCase(
            "[1, {\"a\": 4, \"b\": 5}]",
            "[1, {\"a\": 4, \"b\": 5}]"
        )]
        [TestCase(
            "[1, {\"a\": 4, \"b\": 5}]",
            "[{\"a\": 4, \"b\": 5}, 1]"
        )]
        [TestCase(
            "[1, {\"b\": 5, \"a\": 4}]",
            "[{\"a\": 4, \"b\": 5}, 1]"
        )]
        [TestCase(
            "[1, 1, 2]",
            "[1, 2, 1]"
        )]
        [TestCase(
            "[1, [[2, 3]]]",
            "[[[3, 2]], 1]"
        )]
        [TestCase(
            "\"hello\"",
            "\"hello\""
        )]
        [TestCase(
            "4",
            "4.0"
        )]
        [TestCase(
            "4",
            "4.000000000001"
        )]
        [TestCase(
            "null",
            "null"
        )]
        [TestCase(
            "true",
            "true"
        )]
        [TestCase(
            "false",
            "false"
        )]
        public void AssertAreEquivalent(string expected, string actual)
        {
            var diff = JsonDeepEqual.FindDifferences(expected, actual);
            Assert.IsEmpty(diff);
        }

        [TestCase(
            "{}",
            "{\"a\": 4}",
            "actual.a"
        )]
        [TestCase(
            "{\"a\": 4}",
            "{}",
            "expected.a"
        )]
        [TestCase(
            "{\"a\": 4}",
            "{\"a\": 5}",
            "expected.a"
        )]
        [TestCase(
            "{\"a\": 4}",
            "null",
            "expected"
        )]
        [TestCase(
            "null",
            "{\"a\": 4}",
            "expected"
        )]
        [TestCase(
            "{\"a\": 4}",
            "{\"a\": 4, \"b\": 5}",
            "actual.b"
        )]
        [TestCase(
            "{\"a\": 4, \"b\": 5}",
            "{\"a\": 4}",
            "expected.b"
        )]
        [TestCase(
            "{\"a\": 4, \"b\": 5}",
            "{\"a\": 4, \"b\": 6}",
            "expected.b"
        )]
        [TestCase(
            "{\"a\": 4, \"b\": 6}",
            "{\"a\": 4, \"b\": 5}",
            "expected.b"
        )]
        [TestCase(
            "{\"a\": 5, \"b\": 6}",
            "{\"a\": 4, \"b\": 5}",
            "expected.a",
            "expected.b"
        )]
        [TestCase(
            "[1, 1]",
            "[1]",
            "expected[1]"
        )]
        [TestCase(
            "[1]",
            "[1, 1]",
            "actual[1]"
        )]
        [TestCase(
            "[1, 2]",
            "[1, 1]",
            "actual[1]",
            "expected[1]"
        )]
        [TestCase(
            "[1, 2]",
            "[1, 1, 2]",
            "actual[1]"
        )]
        [TestCase(
            "[1, 2, 3]",
            "[1, 2, 3, 4, 5]",
            "actual[3]",
            "actual[4]"
        )]
        [TestCase(
            "[1, 2, 3, 4, 5]",
            "[1, 2, 3]",
            "expected[3]",
            "expected[4]"
        )]
        [TestCase(
            "[1, 2, 3, 4, 5]",
            "[1, 2, 3]",
            "expected[3]",
            "expected[4]"
        )]
        [TestCase(
            "[1, 2, 3, 4, 5]",
            "[1, 2, 3]",
            "expected[3]",
            "expected[4]"
        )]
        [TestCase(
            "true",
            "false",
            "expected"
        )]
        [TestCase(
            "\"hello\"",
            "\"world\"",
            "expected"
        )]
        [TestCase(
            "foo",
            "\"world\"",
            "expected"
        )]
        [TestCase(
            "\"foo\"",
            "bar",
            "actual"
        )]
        [TestCase(
            "foo",
            "bar",
            "expected",
            "actual"
        )]
        public void AssertAreNotEquivalent(string expected, string actual, params string[] jsonPaths)
        {
            var diff = JsonDeepEqual.FindDifferences(expected, actual).ToList();
            Assert.AreEqual(
                jsonPaths.OrderBy(x => x).ToList(),
                diff.Select(x => $"{x.Target.ToString().ToLower()}{x.Path}").OrderBy(x => x).ToList()
            );
        }

        [TestCase(
            "[1, 2, 3]",
            "[1, 2, 3]"
        )]
        public void AssertAreArrayEquivalent(string expected, string actual)
        {
            var diff = JsonDeepEqual.FindDifferences(expected, actual, new JsonDeepEqualOptions
            {
                IgnoreArrayOrdering = false
            });
            Assert.IsEmpty(diff);
        }


        [TestCase(
            "[1, 2, 3]",
            "[2, 1, 3]",
            "expected[0]",
            "expected[1]"
        )]
        [TestCase(
            "[1, 2, 3]",
            "[1, 2, 3, 4]",
                "actual[3]"
        )]
        [TestCase(
            "[1, 2, 3, 4]",
            "[1, 2, 3]",
            "expected[3]"
        )]
        
        [TestCase(
            "[1, 2, 3]",
            "[1, 2, 3, 3]",
                "actual[3]"
        )]
        [TestCase(
            "[1, 2, 3, 3]",
            "[1, 2, 3]",
            "expected[3]"
        )]
        public void AssertAreNotArrayEquivalent(string expected, string actual, params string[] jsonPaths)
        {
            var diff = JsonDeepEqual.FindDifferences(expected, actual, new JsonDeepEqualOptions
            {
                IgnoreArrayOrdering = false
            }).ToList();
            Assert.AreEqual(
                jsonPaths.OrderBy(x => x).ToList(),
                diff.Select(x => $"{x.Target.ToString().ToLower()}{x.Path}").OrderBy(x => x).ToList()
            );
        }

        [Test]
        public void ManualTestAssertEqual()
        {
            var expected = "{\"a\": 4, \"b\": 5, \"c\": 6, \"d\": [1, 2, 3]}";
            var actual = "{\"a\": 5, \"c\": 6, \"d\": [2, 4]}";

            JsonDeepEqual.AssertEqual(expected, actual, Console.WriteLine);
            Assert.Pass();
        }
    }
}