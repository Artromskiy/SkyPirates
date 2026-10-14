using System;
using System.Text;
using System.Text.Json;

namespace DVG.SkyPirates.Shared.Tools.Json
{
    public static class JsonHash
    {
        public static ulong GetHash(string json)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            return ReadValue(ref reader);
        }

        public static ulong GetHash(ReadOnlyMemory<byte> utf8Json)
        {
            var reader = new Utf8JsonReader(utf8Json.Span);
            return ReadValue(ref reader);
        }

        private static ulong ReadValue(ref Utf8JsonReader reader)
        {
            reader.Read();

            return reader.TokenType switch
            {
                JsonTokenType.StartObject => ReadObject(ref reader),
                JsonTokenType.StartArray => ReadArray(ref reader),
                JsonTokenType.String => HashString(reader.GetString()),
                JsonTokenType.Number => HashNumber(reader.GetDecimal()),
                JsonTokenType.True => 1,
                JsonTokenType.False => 2,
                JsonTokenType.Null => 3,
                _ => 0,
            };
        }

        private static ulong ReadObject(ref Utf8JsonReader reader)
        {
            ulong hash = 0;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string? key = reader.GetString();
                ulong valueHash = ReadValue(ref reader);
                ulong pairHash = HashString(key) ^ valueHash;
                hash ^= pairHash;
            }

            return hash;
        }

        private static ulong ReadArray(ref Utf8JsonReader reader)
        {
            ulong hash = 0;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                ulong valueHash = ReadValue(ref reader);
                hash = hash * 31 + valueHash;
            }

            return hash;
        }


        private static ulong HashString(string? str)
        {
            ulong hash = 0;
            if (!string.IsNullOrWhiteSpace(str))
            {
                foreach (char c in str)
                {
                    hash = (hash << 5) - hash + c;
                }
            }

            return hash;
        }

        private static ulong HashNumber(decimal value)
        {
            int[] bits = decimal.GetBits(value); // god why unity uses old dotnet
            ulong hash = 0;
            foreach (int part in bits)
            {
                hash ^= (ulong)part;
            }

            return hash;
        }
    }
}
