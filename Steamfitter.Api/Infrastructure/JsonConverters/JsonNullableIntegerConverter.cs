// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steamfitter.Api.Infrastructure.JsonConverters
{
    class JsonNullableIntegerConverter : JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.Number:
                    return reader.GetInt32();
                case JsonTokenType.String:
                    var chkValue = reader.GetString();
                    if (string.IsNullOrWhiteSpace(chkValue)) return null;
                    return int.Parse(chkValue, CultureInfo.InvariantCulture);
                default:
                    throw new JsonException($"Unexpected token {reader.TokenType} for nullable int.");
            }
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    writer.WriteStringValue("");
                    break;
                default:
                    writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
                    break;
            }
        }
    }
}
