using Microsoft.Extensions.Options;
using Q.WebAPI.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Q.WebAPI.Services
{
    public interface IAppointmentService
    {
        Task<(ClientAppointment, AppointmentValidation)> CreateAppointmentWithAudio(int userId, IFormFile file);
        Task<(ClientAppointment, AppointmentValidation)> SaveAppointment(int userId, ClientAppointment appointment);
    }
    public class AppointmentService(

        ILogger<AppointmentService> logger,
        ISpeechToTextService speechToTextService,
        ITextToJsonService textToJsonService,
        IValidationService validationService,
        IDataAccessService dataAccess

        ) : IAppointmentService
    {
        public async Task<(ClientAppointment, AppointmentValidation)> CreateAppointmentWithAudio(int userId, IFormFile file)
        {
            var text = await speechToTextService.Transcribe(file);

            if (CheckIfError(text))
            {   
                return new(new ClientAppointment(), new AppointmentValidation { Error = "Audio might be corrupted", IsSuccess = false });
            }


            var appointment = await textToJsonService.ConvertTextToJson(text);
            var validationResult = await validationService.PreliminaryValidation(appointment);
            return (appointment, validationResult);
        }

        public async Task<(ClientAppointment, AppointmentValidation)> SaveAppointment(int userId, ClientAppointment appointment)
        {

            var validationResult = await validationService.ValidateBeforeSaving(appointment);

            if (validationResult.IsSuccess)
            {
                var result = await dataAccess.SaveAppointment(new Models.DbData.Appointment
                {
                    AdditionalInfo = appointment.AdditionalText,
                    Duration = appointment.AppointmentDurationMinutes,
                    AppointmentDate = appointment.AppointmentDate?.UtcDateTime,
                    Name = appointment.Name,
                    UserId = userId
                });
            }
            return (appointment, validationResult);
        }



        private bool CheckIfError(string json)
        {
            try
            {
                var result = JsonSerializer.Deserialize<Errorobject>(json);
                if (result is not null && result.error is not null)
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    public class Errorobject
    {
        public Error error { get; set; }
    }

    public class Error
    {
        public string message { get; set; }
        public string type { get; set; }
        public string param { get; set; }
        public string code { get; set; }
    }
}
