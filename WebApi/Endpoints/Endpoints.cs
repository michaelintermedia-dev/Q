namespace WebApi.Endpoints
{
    public static class Endpoints
    {
        public static void MapEndpoints(this WebApplication app)
        {
            app.MapGet("/hello", () => "Hello, World!");
        }
    }
}
