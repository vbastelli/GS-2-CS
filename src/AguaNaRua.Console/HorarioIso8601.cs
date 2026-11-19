using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AguaNaRua.ConsoleApp;

public static partial class HorarioIso8601
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})$")]
    private static partial Regex Formato();

    public static bool TentarLer(string? texto, out DateTimeOffset instante)
    {
        instante = default;
        return texto is not null && Formato().IsMatch(texto)
            && DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out instante);
    }

    public static string Exibir(DateTimeOffset instante)
        => instante.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture) + " (UTC)";
}

internal sealed class HorarioJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || !HorarioIso8601.TentarLer(reader.GetString(), out var instante))
            throw new JsonException("Timestamp sem formato ISO 8601 com timezone explícito.");
        return instante;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
}
