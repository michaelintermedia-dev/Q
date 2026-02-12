using Q.WebAPI.Models;
using System.Net.Http.Headers;

namespace Q.WebAPI.Services
{
    public interface ISpeechToTextService
    {
        Task<string> Transcribe(IFormFile file);
    }
    public class SpeechToTextService(ILogger<SpeechToTextService> logger, IHttpClientFactory httpClientFactory) : ISpeechToTextService
    {
        public async Task<string> Transcribe(IFormFile file)
        {
            using var client = httpClientFactory.CreateClient(Consts.openAiHttpClient);

            using var form = new MultipartFormDataContent();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var fileBytes = stream.ToArray();
            var fileContent = new ByteArrayContent(fileBytes);

            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mp4");

            form.Add(fileContent, "file", Path.GetFileName(file.FileName));

            form.Add(new StringContent("gpt-4o-transcribe"), "model");

            //form.Add(new StringContent("he"), "language"); // he = Hebrew, ru = Russian

            var response = await client.PostAsync("/v1/audio/transcriptions", form);

            var json = await response.Content.ReadAsStringAsync();

            return json;
        }
    }
}
