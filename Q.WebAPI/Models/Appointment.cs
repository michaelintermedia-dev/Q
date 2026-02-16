using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Q.WebAPI.Models
{
    public class Appointment
    {
        public string Name { get; set; }
        public string Phone { get; set; }

        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime? AppointmentDate { get; set; }

        public int AppointmentDurationMinutes { get; set; }
        public string AdditionalText { get; set; }
    }

    public class FlexibleDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
                return null;

            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("o"));
            else
                writer.WriteNullValue();
        }
    }



    public class OpenAiResponse
    {
        public string id { get; set; }
        public string _object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public Choice[] choices { get; set; }
        public Usage usage { get; set; }
        public string service_tier { get; set; }
        public string system_fingerprint { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
        public Prompt_Tokens_Details prompt_tokens_details { get; set; }
        public Completion_Tokens_Details completion_tokens_details { get; set; }
    }

    public class Prompt_Tokens_Details
    {
        public int cached_tokens { get; set; }
        public int audio_tokens { get; set; }
    }

    public class Completion_Tokens_Details
    {
        public int reasoning_tokens { get; set; }
        public int audio_tokens { get; set; }
        public int accepted_prediction_tokens { get; set; }
        public int rejected_prediction_tokens { get; set; }
    }

    public class Choice
    {
        public int index { get; set; }
        public Message message { get; set; }
        public object logprobs { get; set; }
        public string finish_reason { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
        public object refusal { get; set; }
        public object[] annotations { get; set; }
    }

}
