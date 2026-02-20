using Q.WebAPI.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Q.WebAPI.Services
{
    public interface ITextToJsonService
    {
        Task<ClientAppointment> ConvertTextToJson(string text);
    }
    public class TextToJsonService(ILogger<TextToJsonService> logger, IHttpClientFactory httpClientFactory) : ITextToJsonService
    {
        public async Task<ClientAppointment> ConvertTextToJson(string text)
        {

            var bodyObject = new
            {
                model = "gpt-4.1",
                messages = new[]
                {
                    new {
                        role = "system",
                        content =
                        """
                        You extract appointment booking details from a conversation.
                        Return ONLY valid JSON matching the schema.
                        If some data is missing, still fill the fields as best as possible.
                        Phone must be digits only.
                        All dates must be returned in ISO 8601 format (YYYY-MM-DDTHH:MM:SS), 
                        for example: 2026-02-13T14:30:00.
                        """
                    },
                    new {
                        role = "user",
                        content = $"Transcript:\n{text}"
                    }
                },

                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "appointment",
                        schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                Name = new { type = "string" },
                                Phone = new { type = "string" },
                                AppointmentDate = new { type = "string", format = "date-time" },
                                AppointmentDurationMinutes = new { type = "integer" },
                                AdditionalText = new { type = "string" }
                            },
                            required = new[]
                            {
                                "Name",
                                "Phone",
                                "AppointmentDate",
                                "AppointmentDurationMinutes",
                                "AdditionalText"
                            },
                            additionalProperties = false
                        }
                    }
                }
            };

            var jsonBody = JsonSerializer.Serialize(bodyObject);

            using var client = httpClientFactory.CreateClient(Consts.openAiHttpClient);

            var response = await client.PostAsync("/v1/chat/completions", new StringContent(jsonBody, Encoding.UTF8, "application/json"));

            var responseJson = await response.Content.ReadAsStringAsync();

            var openai = JsonSerializer.Deserialize<OpenAiResponse>(responseJson);

            var appointmentJson = openai?.choices?[0]?.message?.content;

            var appointment =  JsonSerializer.Deserialize<ClientAppointment>(appointmentJson);

            return appointment;

        }
    }
}
