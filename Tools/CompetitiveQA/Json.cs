using System;
using System.Reflection;
using System.Text.Json;
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
            return new JsonSerializerOptions { IncludeFields = true, TypeInfoResolver = resolver, WriteIndented = false, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals };
        }
        public static string To<T>(T value) => JsonSerializer.Serialize(value, Options);
        public static T From<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
        public static string Pretty<T>(T value) => JsonSerializer.Serialize(value, new JsonSerializerOptions(Options) { WriteIndented = true });
    }
}
