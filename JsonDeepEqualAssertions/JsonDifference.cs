namespace JsonDeepEqualAssertions
{
    public class JsonDifference
    {
        public JsonDifference(JsonTarget target, JsonPath path, string reason)
        {
            Target = target;
            Path = path;
            Reason = reason;
        }

        public JsonTarget Target { get; }
        public JsonPath Path { get; }
        public string Reason { get; }

        public override string ToString()
        {
            return $"{Target.ToString().ToLower()}{Path}: {Reason}";
        }
    }
}