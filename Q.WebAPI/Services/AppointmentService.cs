namespace Q.WebAPI.Services
{
    public interface IAppointmentService
    {
        Task<object> CreateAppointmentWithAudio(int userId, IFormFile file);
    }
    public class AppointmentService : IAppointmentService
    {
        public Task<object> CreateAppointmentWithAudio(int userId, IFormFile file)
        {
            throw new NotImplementedException();
        }
    }
}
