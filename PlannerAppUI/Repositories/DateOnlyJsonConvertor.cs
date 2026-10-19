using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlannerApplication.Repositories
{
    internal class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        private const string Format = "yyyy-MM-dd";

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            if (DateOnly.TryParseExact(value, Format, out DateOnly date))
            {
                return date;
            }
            if (DateOnly.TryParse(value, out date))
            {
                return date;
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(Format));
        }
    }
}