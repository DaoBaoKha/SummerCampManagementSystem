using System.Text.Json;
using System.Text.Json.Serialization;

namespace SummerCampManagementSystem.BLL.Helpers
{
    public class VietnamTimeOnlyConverter : JsonConverter<TimeOnly>
    {
        // timeOnly json
        private const string TimeFormat = "HH:mm:ss";

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var timeString = reader.GetString();
            if (TimeOnly.TryParseExact(timeString, TimeFormat, out var vietnamTime))
            {
                return vietnamTime;
            }
            throw new JsonException($"Unable to parse TimeOnly: {timeString}. Expected format: {TimeFormat}");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            // convert to vietnam time for data return
            var vietnamTime = value.ToVietnamTime();

            writer.WriteStringValue(vietnamTime.ToString(TimeFormat));
        }
    }
}