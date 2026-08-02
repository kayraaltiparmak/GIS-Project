using backend.Data;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using backend.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddScoped<ITblPointService, TblPointService>();
builder.Services.AddScoped<ITblLineService, TblLineService>();
builder.Services.AddScoped<ITblPolygonService, TblPolygonService>();
builder.Services.AddScoped<IInventoryAnalysisService, InventoryAnalysisService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();
builder.Services.AddScoped<IGeographicPermissionService, GeographicPermissionService>();
builder.Services.AddScoped<ILocationAnalysisService, LocationAnalysisService>();
builder.Services.AddScoped<ITransportRouteCalculationService, TransportRouteCalculationService>();
builder.Services.AddScoped<ITransportRouteAccessService, TransportRouteAccessService>();
builder.Services.AddOptions<VehicleSimulationOptions>()
    .Bind(builder.Configuration.GetSection(VehicleSimulationOptions.SectionName))
    .Validate(x => x.UpdateIntervalMilliseconds > 0, "VehicleSimulation update interval must be positive.")
    .Validate(x => x.SpeedMetersPerSecond > 0 && double.IsFinite(x.SpeedMetersPerSecond), "VehicleSimulation speed must be positive.")
    .Validate(x => x.CompletedStateRetentionMinutes > 0, "VehicleSimulation retention must be positive.")
    .ValidateOnStart();
builder.Services.AddSingleton<VehicleSimulationService>();
builder.Services.AddSingleton<IVehicleSimulationService>(services => services.GetRequiredService<VehicleSimulationService>());
builder.Services.AddHostedService(services => services.GetRequiredService<VehicleSimulationService>());
builder.Services.AddHttpClient<IOsrmService, OsrmService>((services, client) =>
{
    var baseUrl = services.GetRequiredService<IConfiguration>()["Osrm:BaseUrl"]
        ?? throw new InvalidOperationException("Osrm:BaseUrl configuration is missing.");
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.UseNetTopologySuite()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken)
                    && context.HttpContext.Request.Path.StartsWithSegments("/hubs/vehicle-simulation"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ReactPolicy");

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<VehicleSimulationHub>("/hubs/vehicle-simulation");

await PermissionSeedService.EnsureRequiredPermissionsAsync(app.Services);

app.Run();
