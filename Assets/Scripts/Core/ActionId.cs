using System;

namespace Core
{
    public readonly struct ActionId : IEquatable<ActionId>
    {
        public string Value { get; }

        public ActionId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Value cannot be null or empty", nameof(value));
            }

            Value = value;
        }
        
        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(ActionId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ActionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }

        public static bool operator ==(ActionId left, ActionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActionId left, ActionId right)
        {
            return !left.Equals(right);
        }
    }
}