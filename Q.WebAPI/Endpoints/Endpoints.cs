using Microsoft.AspNetCore.Mvc;
using Q.WebAPI.Services;

namespace Q.WebAPI.Endpoints
{
    public static class Endpoints
    {
        public static void MapEndpoints(this WebApplication app)
        {
            app.MapGet("/hello", () => "Hello, World!");

            app.MapPost("/UploadAudioTest", async (IFormFile file, [FromServices] IAppointmentService appointmentService) =>
            {
                var result = await appointmentService.CreateAppointmentWithAudio(1, file);
                return Results.Ok(result);

            }).DisableAntiforgery();

            app.MapPost("/UploadAudio", async (HttpRequest request, [FromServices] IAppointmentService appointmentService ) =>
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

                //var userIdClaim = request.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                //{
                //    return Results.Unauthorized();
                //}

                var file = form.Files[0];
                var result = await appointmentService.CreateAppointmentWithAudio(1, file);

                //if (!success)
                //{
                //    return Results.BadRequest(new { message });
                //}

                //return Results.Ok(new { message, recordingId });

                return Results.Ok(result);
            });
        }
    }
}
