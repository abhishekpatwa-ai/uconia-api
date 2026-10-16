// uconia-api: a small .NET minimal API that runs on ECS Fargate behind the shared ALB.
// The ALB sends /api/* here; the target group health-checks /health.

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Set by the pipeline: the commit SHA that is running right now
var version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "local";

// Health check for the ALB target group (golden path rule: every service has /health)
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Which version and which task answered (refresh to see different tasks)
app.MapGet("/api/info", () => Results.Ok(new
{
    service = "uconia-api",
    version,
    host = Environment.MachineName,
    time = DateTime.UtcNow
}));

app.MapGet("/api/hello/{name}", (string name) =>
    Results.Ok(new { message = $"Hello {name}, from uconia-api", version }));

app.Run();
