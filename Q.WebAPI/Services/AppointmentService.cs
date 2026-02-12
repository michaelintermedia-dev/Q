using Q.WebAPI.Models;
using System.Net.Http.Headers;

namespace Q.WebAPI.Services
{
    public interface IAppointmentService
    {
        Task<Appointment> CreateAppointmentWithAudio(int userId, IFormFile file);
    }
    public class AppointmentService(

        ILogger<AppointmentService> logger,
        ISpeechToTextService speechToTextService,
        ITextToJsonService textToJsonService

        ) : IAppointmentService
    {
        public async Task<Appointment> CreateAppointmentWithAudio(int userId, IFormFile file)
        {
            var text = await speechToTextService.Transcribe(file);
            var appointment = await textToJsonService.ConvertTextToJson(text);
            return appointment;
        }
    }
}
