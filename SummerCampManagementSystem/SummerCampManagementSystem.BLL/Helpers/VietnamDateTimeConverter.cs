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
            if (string.IsNullOrEmpty(dateString))
            {
                throw new JsonException("Date string cannot be null or empty");
            }

            // Parse the incoming date string
            if (DateTime.TryParse(dateString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var date))
            {
                // Frontend now sends UTC with 'Z' postfix, so keep it as UTC
                // Convert UTC to Vietnam time (UTC+7) for internal processing
                if (date.Kind == DateTimeKind.Utc || dateString.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
                {
                    // Keep as UTC, database will store this correctly
                    return DateTime.SpecifyKind(date, DateTimeKind.Utc);
                }

                // If no timezone specified, treat as UTC
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);
            }

            throw new JsonException($"Unable to parse date: {dateString}");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Database stores UTC, return as UTC with 'Z' postfix
            // Frontend expects UTC timestamps
            DateTime utcTime;

            if (value.Kind == DateTimeKind.Utc)
            {
                utcTime = value;
            }
            else if (value.Kind == DateTimeKind.Unspecified)
            {
                // Assume database stored as UTC
                utcTime = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
            else
            {
                utcTime = value.ToUniversalTime();
            }

            // Return UTC with 'Z' postfix
            writer.WriteStringValue(utcTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }
}