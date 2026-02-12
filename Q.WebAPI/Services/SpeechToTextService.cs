using Q.WebAPI.Models;
using System.Net.Http.Headers;

namespace Q.WebAPI.Services
{
    public interface ISpeechToTextService
    {
        Task<string> Transcribe(IFormFile file);
    }
    public class SpeechToTextService(IHttpClientFactory httpClientFactory) : ISpeechToTextService
    {
        public async Task<string> Transcribe(IFormFile file)
        {
            using var client = httpClientFactory.CreateClient(Consts.openaiHttpClient);

            using var form = new MultipartFormDataContent();

            // читаем аудио
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var fileBytes = stream.ToArray();
            var fileContent = new ByteArrayContent(fileBytes);

            // ВАЖНО: правильный content-type
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mp4");

            // имя "file" — ОБЯЗАТЕЛЬНО
            form.Add(fileContent, "file", Path.GetFileName(file.FileName));

            // модель Whisper
            form.Add(new StringContent("gpt-4o-transcribe"), "model");

            // (необязательно) можно подсказать язык
            //form.Add(new StringContent("he"), "language"); // he = Hebrew, ru = Russian

            // отправка
            var response = await client.PostAsync(
                "/v1/audio/transcriptions",
                form);

            var json = await response.Content.ReadAsStringAsync();

            return json;
        }
    }
}
