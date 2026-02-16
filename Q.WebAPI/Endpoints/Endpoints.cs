using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Q.WebAPI.Models;
using Q.WebAPI.Services;
using System.Security.Claims;

namespace Q.WebAPI.Endpoints
{
    public static class Endpoints
    {
        public static void MapEndpoints(this WebApplication app)
        {
            app.MapGet("/hello", (UserService userService) =>
            {
                return $"Hello, World! {userService.UserId}";
            });

            app.MapPost("/UploadAudioTest", async (IFormFile file, [FromServices] IAppointmentService appointmentService) =>
            {
                var result = await appointmentService.CreateAppointmentWithAudio(1, file);
                return Results.Ok(new { appointment = result.Item1, validation = result.Item2 });

            }).DisableAntiforgery();

            app.MapPost("/UploadAudio", async (HttpRequest request, [FromServices] IAppointmentService appointmentService) =>
            {
                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(new { message = "Expected multipart/form-data request" });
                }

                var form = await request.ReadFormAsync();
                if (form?.Files == null || form.Files.Count == 0)
                {
                    return Results.BadRequest(new { message = "No file provided" });
                }

                var userIdClaim = request.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var file = form.Files[0];
                var result = await appointmentService.CreateAppointmentWithAudio(userId, file);

                return Results.Ok(new { appointment = result.Item1, validation = result.Item2 });
            });

            app.MapPost("/ConfirmAppointment", async (IHttpContextAccessor httpContextAccessor, IAppointmentService appointmentService, [FromBody] Appointment appointment) =>
            {

                var userIdClaim = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }


                var result = await appointmentService.SaveAppointment(userId, appointment);

                return Results.Ok(new { appointment = result.Item1, validation = result.Item2 });
            });
        }
    }
}
