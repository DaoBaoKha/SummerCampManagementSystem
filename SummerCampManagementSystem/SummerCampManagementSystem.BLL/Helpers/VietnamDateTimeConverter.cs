using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SummerCampManagementSystem.BLL.Helpers
{
    public class VietnamDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            if (DateTime.TryParse(dateString, out var date))
            {
                // Assume incoming dates are in Vietnam time
                return date;
            }
            throw new JsonException($"Unable to parse date: {dateString}");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Convert UTC from database to Vietnam time for API response
            var vietnamTime = value.ToVietnamTime();
            writer.WriteStringValue(vietnamTime.ToString("yyyy-MM-ddTHH:mm:ss"));
        }
    }
}