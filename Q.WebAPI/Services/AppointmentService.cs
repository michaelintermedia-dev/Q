using Microsoft.Extensions.Options;
using Q.WebAPI.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Q.WebAPI.Services
{
    public interface IAppointmentService
    {
        Task<(Appointment, AppointmentValidation)> CreateAppointmentWithAudio(int userId, IFormFile file);
        Task<(Appointment, AppointmentValidation)> SaveAppointment(int userId, Appointment appointment);
    }
    public class AppointmentService(

        ILogger<AppointmentService> logger,
        ISpeechToTextService speechToTextService,
        ITextToJsonService textToJsonService,
        IValidationService validationService,
        IDataAccessService dataAccess

        ) : IAppointmentService
    {
        public async Task<(Appointment, AppointmentValidation)> CreateAppointmentWithAudio(int userId, IFormFile file)
        {
            var text = await speechToTextService.Transcribe(file);

            CheckIfError(text);

            var appointment = await textToJsonService.ConvertTextToJson(text);
            var validationResult = await validationService.PreliminaryValidation(appointment);
            return (appointment, validationResult);
        }

        public async Task<(Appointment, AppointmentValidation)> SaveAppointment(int userId, Appointment appointment)
        {

            var validationResult = await validationService.ValidateBeforeSaving(appointment);

            if (validationResult.IsSuccess)
            {
                var result = await dataAccess.SaveAppointment(new Models.DbData.Appointment
                {
                    AdditionalInfo = appointment.AdditionalText,
                    Duration = appointment.AppointmentDurationMinutes,
                    AppointmentDate = appointment.AppointmentDate,
                    Name = appointment.Name,
                    UserId = userId
                });
            }
            return (appointment, validationResult);
        }



        private void CheckIfError(string json)
        {
            try
            {
                var result = JsonSerializer.Deserialize<Errorobject>(json);
                if (result is not null && result.error is not null)
                { 
                
                }

            }
            catch
            {
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
