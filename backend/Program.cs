using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using SmartLearning.Api.Data;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

// The wwwroot/uploads folder is used when Cloudinary is not configured. On a fresh server
// (for example a Docker container) it may not exist yet, so create it before starting.
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"));

var builder = WebApplication.CreateBuilder(args);

// Hosting sites like Render tell the app which port to use in the PORT variable.
// On your own computer PORT is not set, so the app uses launchSettings.json (port 5000).
string port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls("http://0.0.0.0:" + port);
}

// ---------------- Database (PostgreSQL) ----------------
// The connection string is in appsettings.json -> "ConnectionStrings:DefaultConnection"
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// ---------------- Our services ----------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<CourseAccessService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<CouponService>();

// EmailService gets its own HttpClient to call the Brevo email API.
builder.Services.AddHttpClient<EmailService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddControllers();

// ---------------- Login with JWT tokens ----------------
string jwtKey = builder.Configuration["Jwt:Key"];
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // After the token itself is checked, make sure it is still up to date:
        // the user must still exist, and their SecurityStamp must be the same as in the token.
        // (It changes when an admin changes the role, or when the password is changed.)
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                int userId = int.Parse(context.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
                string tokenStamp = context.Principal.FindFirstValue(TokenService.StampClaim) ?? "";

                AppDbContext db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                User user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null || (user.SecurityStamp ?? "") != tokenStamp)
                {
                    context.Fail("This login has expired. Please log in again.");
                }
            }
        };
    });
builder.Services.AddAuthorization();

// ---------------- CORS ----------------
// Allows the React app (a different address/port) to call this API.
// FrontendUrl can hold several addresses separated by commas, for example
// "https://studify.vercel.app,http://localhost:5173".
// A "/" at the end is removed, because the browser sends the address without it.
string[] frontendUrls = builder.Configuration["FrontendUrl"].Split(',');
List<string> allowedOrigins = new List<string>();
foreach (string url in frontendUrls)
{
    string cleanUrl = url.Trim().TrimEnd('/');
    if (cleanUrl != "")
    {
        allowedOrigins.Add(cleanUrl);
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins(allowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ---------------- Rate limiting ----------------
// Stops password guessing and code guessing: each IP address can call the login /
// sign-up / code endpoints at most 20 times per minute. After that it gets "429 Too Many Requests".
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            key => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"message\":\"Too many attempts. Please wait a minute and try again.\"}", token);
    };
});

// ---------------- Running behind a proxy (Railway, Render ...) ----------------
// The hosting site receives the https request and passes it to this app as http.
// These headers tell the app the original address, so links it builds start with https.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// ---------------- Big uploads (videos) ----------------
// Allow requests up to 500 MB. (The Cloudinary FREE plan accepts videos up to 100 MB.)
long maxUploadBytes = 500L * 1024 * 1024;
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxUploadBytes;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxUploadBytes;
});

var app = builder.Build();

// ---------------- Safety checks for a live server ----------------
// On your computer the environment is "Development" and these checks are skipped.
if (!app.Environment.IsDevelopment())
{
    // The default key is public on GitHub. With it, anyone could create a fake admin login.
    if (jwtKey.StartsWith("CHANGE-ME"))
    {
        throw new Exception("Set the Jwt__Key environment variable to your own long secret.");
    }

    string cloudName = app.Configuration["Cloudinary:CloudName"];
    if (string.IsNullOrWhiteSpace(cloudName) || cloudName.StartsWith("YOUR_"))
    {
        app.Logger.LogWarning("Cloudinary is not configured. Uploaded files are saved on this server's disk "
            + "and are LOST on every redeploy. Set the Cloudinary__CloudName, Cloudinary__ApiKey and "
            + "Cloudinary__ApiSecret environment variables.");
    }

    string brevoKey = app.Configuration["Email:BrevoApiKey"];
    if (string.IsNullOrWhiteSpace(brevoKey) || brevoKey.StartsWith("YOUR_"))
    {
        app.Logger.LogWarning("Brevo is not configured, so NO emails are sent (codes appear in this log instead). "
            + "Set the Email__BrevoApiKey and Email__SenderEmail environment variables.");
    }
}

// ---------------- Create / update the database, then add demo data ----------------
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();   // creates the database and tables if they do not exist

    string demoPassword = app.Configuration["DemoPassword"];

    // On a live server, never create the admin account with the public demo password.
    bool databaseIsEmpty = !db.Users.Any();
    if (!app.Environment.IsDevelopment() && databaseIsEmpty && demoPassword == "Password@123")
    {
        throw new Exception("Set the DemoPassword environment variable before the first start. "
            + "It becomes the password of admin@smartlearn.dev.");
    }

    DbSeeder.Seed(db, demoPassword);   // adds demo users, categories, plans and courses (only once)
}

// ---------------- Request pipeline ----------------
app.UseForwardedHeaders();
app.UseStaticFiles();        // serves wwwroot/uploads (used when Cloudinary is not configured)
app.UseCors("ReactApp");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Opening the backend address in a browser shows this, so you can see the API is running.
app.MapGet("/", () => "SmartLearn API is running. Try /api/courses");

app.Run();
