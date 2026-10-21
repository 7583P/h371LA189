using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace CompetitiveQA
{
    // Mirrors Unity JsonUtility closely enough for the journal: public fields,
    // [NonSerialized] fields skipped, properties ignored.
    public static class Json
    {
        static readonly JsonSerializerOptions Options = Create();
        static JsonSerializerOptions Create()
        {
            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(info =>
            {
                if (info.Kind != JsonTypeInfoKind.Object || info.Type.Namespace == null || !info.Type.Namespace.StartsWith("AnimeHillRacing")) return;
                for (int i = info.Properties.Count - 1; i >= 0; i--)
                {
                    var member = info.Properties[i].AttributeProvider as MemberInfo;
                    if (member is PropertyInfo) { info.Properties.RemoveAt(i); continue; }
                    if (member is FieldInfo field && (field.IsNotSerialized || field.IsStatic || field.IsInitOnly)) info.Properties.RemoveAt(i);
                }
            });
            var options = new JsonSerializerOptions { IncludeFields = true, TypeInfoResolver = resolver, WriteIndented = false, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };
            options.Converters.Add(new UnityByteArrayReader());
            return options;
        }
        // Unity writes byte[] as numeric arrays; older CLI fixtures use base64.
        // Accept both without rewriting a native journal or changing CLI output.
        sealed class UnityByteArrayReader : JsonConverter<byte[]>
        {
            public override byte[] Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String) return reader.GetBytesFromBase64();
                if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected a Unity byte array or legacy base64 string.");
                var values = new List<byte>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray) return values.ToArray();
                    if (reader.TokenType != JsonTokenType.Number || !reader.TryGetByte(out byte value))
                        throw new JsonException("A Unity byte array must contain integers from 0 to 255.");
                    values.Add(value);
                }
                throw new JsonException("Unterminated Unity byte array.");
            }
            public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
                => writer.WriteBase64StringValue(value);
        }
        public static string To<T>(T value) => JsonSerializer.Serialize(value, Options);
        public static T From<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
        public static string Pretty<T>(T value) => JsonSerializer.Serialize(value, new JsonSerializerOptions(Options) { WriteIndented = true });
    }
}
