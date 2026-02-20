using Q.WebAPI.Models;
using System.Net.Http.Headers;

namespace Q.WebAPI.Services
{
    public interface IValidationService
    {
        Task<AppointmentValidation> ValidateBeforeSaving(ClientAppointment appointment);
        Task<AppointmentValidation> PreliminaryValidation(ClientAppointment appointment);
    }
    public class ValidationService(

        ILogger<ValidationService> logger,
        IDataAccessService dataAccessService,
        UserService userService

        ) : IValidationService
    {
        public async Task<AppointmentValidation> PreliminaryValidation(ClientAppointment appointment)
        {
            return await Validate(appointment);
        }

        public async Task<AppointmentValidation> ValidateBeforeSaving(ClientAppointment appointment)
        {
            return await Validate(appointment);
        }

        private async Task<AppointmentValidation> Validate(ClientAppointment appointment)
        {
            if (appointment.AppointmentDate is not { } newStart)
                return new AppointmentValidation { IsSuccess = false, Error = "Appointment date is required." };

            var newEnd = newStart.UtcDateTime.AddMinutes(appointment.AppointmentDurationMinutes);

            var existing = await dataAccessService.GetAppointmentsByUserId(userService.UserId);
            // Note: we need the userId here — see caveat below

            foreach (var ex in existing)
            {
                if (ex.AppointmentDate is not { } exStart || ex.Duration is not { } exDuration)
                    continue;

                var exEnd = new DateTimeOffset(exStart).UtcDateTime.AddMinutes(exDuration);

                if (newStart.UtcDateTime < new DateTimeOffset(exStart).UtcDateTime && new DateTimeOffset(exStart).UtcDateTime < new DateTimeOffset(newEnd).UtcDateTime)
                {
                    return new AppointmentValidation
                    {
                        IsSuccess = false,
                        Error = $"Appointment overlaps with existing appointment '{ex.Name}' on {exStart:g} ({exDuration} min)."
                    };
                }
            }

            return new AppointmentValidation { IsSuccess = true, Error = string.Empty };
        }
    }
}
