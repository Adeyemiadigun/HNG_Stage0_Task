HNGProj — Profile Endpoint (Stage 0)
Welcome to Stage 0! 🎯

This repository implements a simple RESTful GET endpoint that returns profile information together with a dynamic cat fact fetched from the Cat Facts API. This README is tailored for a C# / ASP.NET Core implementation.

Repository: https://github.com/Shaqoo/HNGProj

Contents

What this does
Required endpoint & response format
Quick demo (curl)
Setup & run locally (C# / .NET)
Dependencies & installation
Configuration (environment variables & appsettings)
Implementation notes & example code (Minimal API + Controller options)
Error handling, timeouts & best practices
Testing
Docker (optional)
Troubleshooting
Acceptance checklist
What this does

Exposes a GET /me endpoint that returns JSON:
status: "success"
user: { email, name, stack }
timestamp: current UTC time in ISO 8601 format
fact: random cat fact fetched from https://catfact.ninja/fact
Fetches a fresh cat fact on every request (not cached).
Handles external API errors gracefully and returns a fallback message when necessary.
Returns Content-Type: application/json and appropriate HTTP status codes.
Required endpoint and exact response format Your GET /me endpoint must return JSON exactly shaped as:

{ "status": "success", "user": { "email": "", "name": "", "stack": "" }, "timestamp": "<current UTC time in ISO 8601 format>", "fact": "" }

status — always the string "success"
user.email — your personal email address
user.name — your full name
user.stack — your backend technology stack (e.g., "C#/ASP.NET Core")
timestamp — current UTC time in ISO 8601 format (e.g., "2025-10-19T12:34:56.789Z")
fact — a random cat fact fetched from the Cat Facts API
Quick demo (curl)

After running locally (default port 5000 or as configured):
curl -i http://localhost:5000/me curl -H "Accept: application/json" http://localhost:5000/me

You should see a 200 OK and the JSON payload above. Content-Type must be application/json.

Setup & run locally (C# / ASP.NET Core) Prerequisites

.NET SDK 6.0 or later (recommend .NET 8 if available)
Git
Optional: Docker
Clone the repo

git clone https://github.com/Shaqoo/HNGProj.git
cd HNGProj
Install dependencies & run

Restore and run with dotnet:
dotnet restore
dotnet run --project src/HNGProj.Api
(Adjust project path/name as needed — if the project root is the API project, run dotnet run.)

Build and run:
dotnet build
dotnet run
Typical dev ports

ASP.NET Core defaults: http://localhost:5000 (HTTP) and https://localhost:5001 (HTTPS).
You can set the port with ASPNETCORE_URLS environment variable, e.g.:
export ASPNETCORE_URLS="http://localhost:5000"
(Windows PowerShell: $env:ASPNETCORE_URLS = "http://localhost:5000")

Dependencies and how to install them

.NET SDK — install from https://dotnet.microsoft.com/download
Recommended NuGet packages (these will usually be in the project file):
Microsoft.AspNetCore.App (included with the SDK)
Microsoft.Extensions.Http (for typed HttpClient usage)
Microsoft.Extensions.Configuration.EnvironmentVariables / .Json (for config)
Swashbuckle.AspNetCore (optional - for Swagger)
Serilog or Microsoft.Extensions.Logging (for logging)
If you have package references in the csproj, simply running dotnet restore will install them.
Configuration (environment variables & appsettings)

Environment variables supported (examples):
USER_EMAIL — your email (used in response user.email)
USER_NAME — your full name
USER_STACK — e.g., "C#/ASP.NET Core"
CATFACTS_URL — https://catfact.ninja/fact (default)
CATFACT_TIMEOUT_MS — HTTP request timeout (ms), recommended 2000
FALLBACK_FACT — fallback fact string when external API fails
LOG_LEVEL — info, debug, error
ASPNETCORE_URLS — e.g., http://localhost:5000
Example appsettings.json (optional)

{
  "CatFacts": {
    "Url": "https://catfact.ninja/fact",
    "TimeoutMs": 2000,
    "FallbackFact": "Unable to fetch a cat fact at the moment. Please try again later."
  },
  "User": {
    "Email": "you@example.com",
    "Name": "Your Full Name",
    "Stack": "C#/ASP.NET Core"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
Implementation notes & example code Below are example implementation approaches: Minimal API (Program.cs) and Controller-based. The examples show best practices for using IHttpClientFactory, timeouts, cancellation tokens, and graceful fallback.

Minimal API (Program.cs) — concise example
// Program.cs (Minimal API)
var builder = WebApplication.CreateBuilder(args);

// Configuration and logging (builder.Configuration)
builder.Services.AddHttpClient("catfacts", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CatFacts:Url"] ?? "https://catfact.ninja/fact");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
.SetHandlerLifetime(TimeSpan.FromMinutes(5)); // keep-alive behavior

var app = builder.Build();

app.MapGet("/me", async (IConfiguration config, IHttpClientFactory httpFactory, CancellationToken ct) =>
{
    var user = new
    {
        email = config["User:Email"] ?? Environment.GetEnvironmentVariable("USER_EMAIL") ?? "you@example.com",
        name = config["User:Name"] ?? Environment.GetEnvironmentVariable("USER_NAME") ?? "Your Full Name",
        stack = config["User:Stack"] ?? Environment.GetEnvironmentVariable("USER_STACK") ?? "C#/ASP.NET Core"
    };

    string fallback = config["CatFacts:FallbackFact"] ?? "Unable to fetch a cat fact at the moment. Please try again later.";
    int timeoutMs = int.TryParse(config["CatFacts:TimeoutMs"], out var tm) ? tm : 2000;

    string fact = fallback;

    try
    {
        var client = httpFactory.CreateClient("catfacts");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));
        var response = await client.GetAsync("", cts.Token);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cts.Token);
        if (json.TryGetProperty("fact", out var f) && f.ValueKind == JsonValueKind.String)
        {
            fact = f.GetString() ?? fallback;
        }
    }
    catch (OperationCanceledException) { /* timeout */ }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to fetch cat fact, using fallback.");
    }

    var result = new
    {
        status = "success",
        user,
        timestamp = DateTime.UtcNow.ToString("o"),
        fact
    };

    return Results.Json(result);
});

app.Run();
Controller-based approach (MeController.cs) — outline
[ApiController]
[Route("[controller]")]
public class MeController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<MeController> _logger;

    public MeController(IHttpClientFactory httpFactory, IConfiguration config, ILogger<MeController> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    [HttpGet("/me")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = new { /* same as above */ };
        // fetch fact with timeout and fallback (same pattern)
        // return Ok(object) -> ASP.NET will set Content-Type: application/json
    }
}
Important behavior to implement in code

Use IHttpClientFactory, configure a sensible timeout (e.g., 1500–3000 ms).
Use CancellationToken to enforce timeouts.
On external API failure or timeout, return 200 OK with the same JSON schema and fact set to FALLBACK_FACT (keeps grader happy). Logging should record the failure.
Ensure the timestamp uses DateTime.UtcNow.ToString("o") or DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") to produce ISO 8601.
Error handling, timeouts & best practices

Use typed or named HttpClient via IHttpClientFactory and configure timeout via CancellationToken or HttpClient.Timeout.
Handle: timeouts (OperationCanceledException), non-success status codes (EnsureSuccessStatusCode), JSON parse errors.
Graceful fallback: When the Cat Facts API fails, set fact to config fallback string and still return 200 with proper schema.
Logging: log the error message and stack at warning/error level.
CORS: add app.UseCors(...) if calling from browser.
Rate limiting: consider express-like rate limit (e.g., AspNetCoreRateLimit) for public deployment.
Testing

Manual:

Start the app, then: curl -H "Accept: application/json" http://localhost:5000/me
Confirm:
Response code 200
Content-Type: application/json
JSON contains status, user.email, user.name, user.stack, timestamp, and fact
timestamp is ISO 8601 and varies between requests
Automated (suggested):

Use xUnit + Microsoft.AspNetCore.Mvc.Testing + System.Text.Json for assertions.
Assert Content-Type, schema, and that timestamp differs between two requests.
Docker (optional)

Example minimal Dockerfile:
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/HNGProj.Api/HNGProj.Api.csproj"
RUN dotnet publish "src/HNGProj.Api/HNGProj.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HNGProj.Api.dll"]
Troubleshooting

If you get JSON parsing errors while reading the cat facts response, log the raw response body for debugging.
If running on Windows and env vars don't show up, check PowerShell vs cmd variable syntax.
If port conflict occurs, update ASPNETCORE_URLS or launchSettings.json.
Acceptance checklist

 GET /me endpoint accessible and returns 200 OK
 Response structure strictly follows the required JSON schema
 All required fields present: status, user (email/name/stack), timestamp, fact
 timestamp is current UTC ISO 8601 and updates per request
 fact fetched from Cat Facts API on every request (or fallback on failure)
 Response Content-Type header is application/json
 Code follows C#/.NET best practices: IHttpClientFactory, timeouts, logging
Where to set the user info

The user fields can be set in appsettings.json or via environment variables USER_EMAIL, USER_NAME, USER_STACK. The example code reads these configuration values.
Contact / Maintainer

Repository: https://github.com/Shaqoo/HNGProj
Maintainer: @Shaqoo
