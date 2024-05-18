using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using XeonPwm.Api.Auth;
using XeonPwm.Api.Contexts;
using XeonPwm.Api.Hubs;
using XeonPwm.Api.Models.Db;
using XeonPwm.Api.Services;
using XeonPwm.Api.Services.Impl;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var scheme = new OpenApiSecurityScheme()
    {
        In = ParameterLocation.Header,
        Description = "Enter the auth token",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    };
    options.AddSecurityDefinition("Token", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme()
            {
                Reference = new OpenApiReference()
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Token"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddSingleton<IPwmDriver, PwmDriver>();
builder.Services.AddControllers();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
builder.Services.AddSingleton<ITemperatureReader, LmSensorsReader>();
builder.Services.AddSingleton<IAutoDriver, EfCoreAutoDriver>();
builder.Services.AddDbContextFactory<XeonPwmContext>(
    options => options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")));

builder.Services.AddSingleton<ITokenCache, InMemoryTokenCache>();
builder.Services.AddTransient<IPasswordHasher, PbkdfPasswordHasher>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = TokenAuthSchemeDefaults.TokenAuthScheme;
        options.DefaultAuthenticateScheme = TokenAuthSchemeDefaults.TokenAuthScheme;
        options.DefaultChallengeScheme = TokenAuthSchemeDefaults.TokenAuthScheme;
    })
    .AddScheme<TokenAuthSchemeOptions, TokenAuthHandler>(TokenAuthSchemeDefaults.TokenAuthScheme, options =>
    {
    });

#if DEBUG
builder.Services.AddCors(corsOptions =>
{
    corsOptions.AddDefaultPolicy(cBuilder =>
    {
        cBuilder
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
#endif

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<XeonPwmContext>();
    await context.Database.MigrateAsync();
    await context.SaveChangesAsync();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<XeonPwmContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    if (!await context.Users.AnyAsync())
    {
        var user = new User()
        {
            Username = "admin",
            Hash = hasher.Hash("123")
        };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
    }
    
    var driver = scope.ServiceProvider.GetRequiredService<IPwmDriver>();
    var settings = await driver.Driver.GetPwmSettingsAsync();
    
    app.Logger.LogInformation(
        "PWM Driver settings:\nFrequency: {Frequency} Hz, Resolution: {Resolution}, Max duty cycle: {MaxDtCycle}, Channel: {Channel}, Pin: {Pin}",
        settings.Frequency, settings.Resolution, settings.MaxDutyCycle, settings.Channel, settings.Pin);

    var reader = (LmSensorsReader)scope.ServiceProvider.GetRequiredService<ITemperatureReader>();
    reader.StartReading();

    var autoDriver = (EfCoreAutoDriver)scope.ServiceProvider.GetRequiredService<IAutoDriver>();
    if (!await context.DriverPoints.AnyAsync())
    {
        var points = new RegisteredAutoDriverPoint[]
        {
            new()
            {
                Temperature = 15,
                PwmPercentage = 10
            },
            new()
            {
                Temperature = 50,
                PwmPercentage = 20
            },
        };

        await context.DriverPoints.AddRangeAsync(points);
        await context.SaveChangesAsync();
    }

    await autoDriver.ReloadFromDatabaseAsync();
    
    if (app.Configuration.GetRequiredSection("AutoMode").GetValue<bool>("AutoEnable"))
    {
        autoDriver.Enable = true;
        app.Logger.LogInformation("Auto driver enabled");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<PwmHub>("/hubs/pwm");

app.Run();