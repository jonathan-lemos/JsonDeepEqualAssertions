namespace JsonDeepEqualAssertions
{
    public class JsonDeepEqualOptions
    {
        /// <summary>
        /// Set to false to respect the order of arrays ([1, 2] != [2, 1]). By default arrays are treated as unordered ([1, 2] == [2, 1]).
        /// </summary>
        public bool IgnoreArrayOrdering { get; set; } = true;
    }
}