// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steamfitter.Api.Infrastructure.JsonConverters
{
    /// <summary>
    /// availableCommands.json declares checkbox parameters with quoted-string defaults
    /// (e.g. "default": "false"), so values arrive as JSON strings rather than booleans.
    /// Accept booleans, strings ("true"/"false"/""), and numbers (0/non-zero).
    /// </summary>
    class JsonFlexibleBoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Null:
                    return false;
                case JsonTokenType.String:
                    var s = reader.GetString();
                    if (string.IsNullOrWhiteSpace(s)) return false;
                    if (bool.TryParse(s, out var b)) return b;
                    throw new JsonException($"Cannot convert '{s}' to bool");
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var n)) return n != 0;
                    throw new JsonException("Cannot convert numeric value to bool");
                default:
                    throw new JsonException($"Unexpected token {reader.TokenType} for bool");
            }
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            => writer.WriteBooleanValue(value);
    }
}
