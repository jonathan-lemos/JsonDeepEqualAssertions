# JsonDeepEqualAssertions
A JSON Deep Equal library for unit tests that works well.

## Features
* Returns all of the differences between the two JSON strings instead of stopping on the first one.
* Highlights the differences between the two JSON strings in color (if using `AssertEqual`).
* Ignores the ordering of arrays by default.

## Example usage
To see the differences between two JSON strings:
```cs
var differences = JsonDeepEqual.FindDifferences("[1, 2, 3]", "[2, 3, 1]");
// this for loop should not run
foreach (var diff in differences) {
    Console.WriteLine(diff);
}
```

To see the differences between two JSON strings while respecting the order of arrays:
```cs
var differences = JsonDeepEqual.FindDifferences("[1, 2, 3]", "[1, 3, 2]");
// this for loop should run twice
foreach (var diff in differences) {
    Console.WriteLine(diff);
}
```

To assert that two JSON strings are equal, and call `Assert.Fail` if they aren't:
```cs
JsonDeepEqual.AssertEqual(expectedJson, actualJson, Assert.Fail);
```