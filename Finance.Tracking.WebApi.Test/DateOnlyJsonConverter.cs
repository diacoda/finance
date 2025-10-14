using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Finance.Tracking.Tests;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private readonly string _format = "yyyy-MM-dd";

    // Deserialize from JSON string to DateOnly
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString() ?? throw new JsonException("Expected string for DateOnly");
        return DateOnly.ParseExact(str, _format);
    }

    // Serialize from DateOnly to JSON string
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(_format));
    }
}
