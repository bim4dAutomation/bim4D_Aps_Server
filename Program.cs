using APS_Automation_Server.Controllers;
using APS_Automation_Server.Data;

using APS_Automation_Server.Services;
using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);

var clientId = builder.Configuration["Autodesk:ClientId"]!;
var clientSecret = builder.Configuration["Autodesk:ClientSecret"]!;
var callbackUri = builder.Configuration["Autodesk:CallbackUrl"]!;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApsDBContext>(options =>
    options.UseNpgsql(conn)
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(callbackUri))
{
    throw new ApplicationException("Missing required environment variables APS_CLIENT_ID, APS_CLIENT_SECRET, or APS_CALLBACK_URL.");
}

builder.Services.AddSingleton(new ApsAuthService(clientId, clientSecret, callbackUri));
builder.Services.AddScoped<ApsObjectService>();
builder.Services.AddScoped<ApsDerivativeService>();
builder.Services.AddScoped<APSHubService>();



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseSession();
app.UseAuthorization();
app.MapControllers();

app.Run();


