using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using RadarLights;
using RadarLights.Services;
using RadarLights.Services.HomeAssistant;
using RpiLedMatrix;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("radarlights.log")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    builder.Configuration.AddJsonFile("appsettings.user.json", true);
    var appConfig = builder.Configuration.Get<AppConfig>();
    appConfig!.Log();
    builder.Services.AddSingleton(appConfig);

    if (appConfig.HomeLongitude == 0 || appConfig.HomeLatitude == 0)
    {
        Console.WriteLine("HomeLatitude and HomeLongitude must be set in appsettings.user.json");
        return -1;
    }

    builder.Services.Configure<HostOptions>(hostOptions => { hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore; });

    builder.Services.AddHttpClient();
    builder.Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
    builder.Services.AddSingleton<AirplaneDataCache>();
    builder.Services.AddSingleton<AirplaneColourService>();
    builder.Services.AddSingleton<OverheadAlertService>();
    builder.Services.AddSingleton(new LedMatrix(new RGBLedMatrix(appConfig.Matrix)));

    builder.Services.AddSingleton<PlaneRenderService>();
    builder.Services.AddHostedService<PlaneRenderService>(p => p.GetRequiredService<PlaneRenderService>());
    builder.Services.AddHostedService<AirplaneDataService>();

    if (appConfig.Mqtt.Enabled)
    {
        builder.Services.AddSingleton<MqttPublisher>();
        builder.Services.AddHostedService<HomeAssistantService>();
    }
    
    builder.Services.AddCors();

    var app = builder.Build();

    app.UseCors(corsPolicyBuilder => corsPolicyBuilder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

    app.UseSerilogRequestLogging();

    app.MapGet("/", () => "Welcome to RadarLights!");
    
    app.MapGet("/config", RadarSettings.Load);

    app.MapPost("/config", ([FromBody] RadarSettings settings) => settings.Save());

    var renderer = app.Services.GetRequiredService<PlaneRenderService>();
    if (RadarSettings.Load().Enabled == false)
    {
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            Console.WriteLine("Radar Disabled. Stopping renderer");
            await renderer.StopAsync(CancellationToken.None);
        });
    }
    
    RadarSettings.SettingsUpdated += async (sender, _) =>
    {
        var settings = (RadarSettings) sender!;
        if (settings.Enabled == RadarSettings.Load().Enabled)
            return;
        if (settings.Enabled == false)
        {
            Console.WriteLine("Stopping renderer");
            await renderer.StopAsync(CancellationToken.None);
        }
        else
        {
            Console.WriteLine("Starting renderer");
            await renderer.StartAsync(CancellationToken.None);
        }
    };

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

return 0;
