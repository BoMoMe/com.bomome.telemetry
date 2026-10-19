using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bomome.Telemetry
{
    [Flags]
    public enum TelemetryCategory
    {
        None = 0, Session = 1, Progression = 2, Tutorial = 4,
        Economy = 8, Monetization = 16, Design = 32, All = 63
    }

    /// <summary>Immutable, bounded envelope. Do not include names, emails or other PII.</summary>
    public sealed class TelemetryEvent
    {
        public string Id { get; }
        public string Name { get; }
        public TelemetryCategory Category { get; }
        public DateTime TimestampUtc { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }

        public TelemetryEvent(string name, TelemetryCategory category,
            IDictionary<string, object> properties = null)
        {
            ValidateKey(name);
            int bits = (int)category;
            if (bits <= 0 || (bits & (bits - 1)) != 0 || (category & TelemetryCategory.All) == 0)
                throw new ArgumentOutOfRangeException(nameof(category), "Choose one category.");
            if (properties != null && properties.Count > 24)
                throw new ArgumentException("At most 24 properties are supported.", nameof(properties));
            var copy = new Dictionary<string, object>(StringComparer.Ordinal);
            if (properties != null)
                foreach (var pair in properties)
                {
                    ValidateKey(pair.Key);
                    object value = pair.Value;
                    if (!(value is string || value is bool || value is int || value is long ||
                          value is float || value is double))
                        throw new ArgumentException("Properties must be non-null scalar values.");
                    if (value is string text && text.Length > 256)
                        throw new ArgumentException("Property strings are limited to 256 characters.");
                    if (value is double d && (double.IsNaN(d) || double.IsInfinity(d)) ||
                        value is float f && (float.IsNaN(f) || float.IsInfinity(f)))
                        throw new ArgumentException("Property numbers must be finite.");
                    copy.Add(pair.Key, value);
                }
            Id = Guid.NewGuid().ToString("N");
            Name = name;
            Category = category;
            TimestampUtc = DateTime.UtcNow;
            Properties = new ReadOnlyDictionary<string, object>(copy);
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length > 64)
                throw new ArgumentException("Event/property names need 1-64 characters.");
            foreach (char c in key)
                if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' ||
                      c >= '0' && c <= '9' || c == '_'))
                    throw new ArgumentException("Event/property names use letters, digits and underscores.");
        }
    }
}
