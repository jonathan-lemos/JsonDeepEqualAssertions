using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonDeepEqualAssertions
{
    public class JsonPath : IEquatable<JsonPath>
    {
        public JsonPath()
        {
        }

        private JsonPath(IReadOnlyList<string> components)
        {
            Components = components;
        }
        
        public IReadOnlyList<string> Components { get; } = new List<string>();

        public JsonPath WithAddedPart(string part)
        {
            var newComponents = new List<string>(Components) {part};
            return new JsonPath(newComponents);
        }

        public override string ToString()
        {
            return $"{string.Join("", Components)}";
        }

        public bool Equals(JsonPath other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Components.SequenceEqual(other.Components);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((JsonPath) obj);
        }

        public override int GetHashCode()
        {
            if (Components == null)
            {
                return 0;
            }

            return Components.Aggregate(0, (a, c) =>
            {
                unchecked
                {
                    return a * 31 + c.GetHashCode();
                }
            });
        }

        public static bool operator ==(JsonPath left, JsonPath right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(JsonPath left, JsonPath right)
        {
            return !Equals(left, right);
        }
    }
}