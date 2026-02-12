using Microsoft.AspNetCore.HttpLogging;
using Q.WebAPI.Endpoints;
using Q.WebAPI.Models;
using Q.WebAPI.Services;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Logging.AddFilter("Microsoft.AspNetCore.HttpLogging", LogLevel.Information);
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
    logging.MediaTypeOptions.AddText("application/json");
});

builder.Services.AddHttpClient(Consts.openaiHttpClient, client =>
{
    client.BaseAddress = new Uri("https://api.openai.com");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", builder.Configuration["OpenAiApiKey"]);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddAuthorization();


builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<ISpeechToTextService, SpeechToTextService>();
builder.Services.AddScoped<ITextToJsonService, TextToJsonService>();

var app = builder.Build();
app.UseHttpLogging();
app.UseExceptionHandler(_ => { });

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}


app.UseCors("AllowAll");

app.UseAuthorization();

app.MapEndpoints();

app.Run();
