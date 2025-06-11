using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Ae.Sample.Mcp.Tools;
using Ae.Sample.Mcp.Services;
using Ae.Sample.Mcp.Settings;
using Ae.Sample.Mcp.Profiles;

Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Verbose()
           .WriteTo.Debug()
           .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose)
           .CreateBootstrapLogger();

try
{
    Log.Information("App starting ...");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.Configure<AppOptions>(
                builder.Configuration.GetSection(AppOptions.App));
    builder.Services.Configure<IdentityStorageApiOptions>(
                builder.Configuration.GetSection(IdentityStorageApiOptions.IdentityStorageApi));

    builder.Services.AddAutoMapper(m =>
    {
        m.AddProfile<DataProfile>();
    });

    builder.Services.AddScoped<IDtoValidator, DtoValidator>();
    builder.Services.AddHttpClient<IClaimClient, ClaimClient>();

    // Resolve AppOptions to get the version for ServerInfo
    var appOptions = builder.Configuration.GetSection(AppOptions.App).Get<AppOptions>() ?? new AppOptions();
    Log.Information($"{appOptions.Name} ver:{appOptions.Version}");

    builder.Services
        .AddMcpServer(options =>
        {
            options.ServerInfo = new() { Name = appOptions.Name, Version = appOptions.Version };
            options.ServerInstructions = "Manage identity claims.";
        })
        .WithStdioServerTransport()
        .WithTools([typeof(ClaimTools), typeof(AppInfoTool)]);

    var app = builder.Build();

    await app.RunAsync();
    return 0;
}
catch (Exception exc)
{
    Log.Fatal(exc, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
