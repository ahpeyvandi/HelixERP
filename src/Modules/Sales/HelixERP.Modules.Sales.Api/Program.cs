var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    module = "Sales",
    utc = DateTime.UtcNow
}));

app.Run();

public partial class Program;