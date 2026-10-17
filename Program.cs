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

// Database check: connects to RDS PostgreSQL and returns its version.
// DB_HOST/PORT/NAME come from the task definition; DB_USER/PASSWORD are injected
// from Secrets Manager by ECS at start-up, so the password never appears in code or config.
app.MapGet("/api/db", async () =>
{
    var host = Environment.GetEnvironmentVariable("DB_HOST");
    if (string.IsNullOrEmpty(host))
        return Results.Problem("Database is not configured for this task", statusCode: 503);

    var connectionString = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = int.Parse(Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"),
        Database = Environment.GetEnvironmentVariable("DB_NAME"),
        Username = Environment.GetEnvironmentVariable("DB_USER"),
        Password = Environment.GetEnvironmentVariable("DB_PASSWORD"),
        SslMode = Npgsql.SslMode.Require, // encrypted in transit
        Timeout = 5
    }.ConnectionString;

    try
    {
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand("select version()", connection);
        var dbVersion = (string?)await command.ExecuteScalarAsync();
        return Results.Ok(new { database = "connected", dbVersion, version, host = Environment.MachineName });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database connection failed: {ex.Message}", statusCode: 503);
    }
});

app.Run();
