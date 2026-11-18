using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AguaNaRua.Api.Api;

// Exige timezone explícito: não interpreta silenciosamente a hora local do servidor.
public sealed partial class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})$")]
    private static partial Regex IsoComOffset();

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var texto = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        if (texto is null || !IsoComOffset().IsMatch(texto)
            || !DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
            throw new JsonException("Use um timestamp ISO 8601 com Z ou offset explícito.");
        return data.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
}
