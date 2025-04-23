using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoanCalculator.Core.Services;

public class DoubleDefaultConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (stringValue == "Infinity") return double.PositiveInfinity;
            if (stringValue == "-Infinity") return double.NegativeInfinity;
            if (stringValue == "NaN") return double.NaN;
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out double value))
        {
            return value;
        }

        return 0.0; // Default value for invalid or missing numbers
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsPositiveInfinity(value))
        {
            writer.WriteStringValue("Infinity");
        }
        else if (double.IsNegativeInfinity(value))
        {
            writer.WriteStringValue("-Infinity");
        }
        else if (double.IsNaN(value))
        {
            writer.WriteStringValue("NaN");
        }
        else
        {
            writer.WriteNumberValue(value);
        }
    }
}
