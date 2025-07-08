﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Ae.Sample.Mcp.Tools;
using Ae.Sample.Mcp.Services;
using Ae.Sample.Mcp.Settings;
using Ae.Sample.Mcp.Profiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Ae.Sample.Mcp.Authentication;

Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Verbose()
           .WriteTo.Debug()
           .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose)
           .CreateBootstrapLogger();

try
{
    Log.Information("App starting ...");
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.Configure<AppOptions>(
                builder.Configuration.GetSection(AppOptions.App));
    builder.Services.Configure<ServerAuthenticationOptions>(
                builder.Configuration.GetSection(ServerAuthenticationOptions.Authentication));
    builder.Services.Configure<IdentityStorageApiOptions>(
                builder.Configuration.GetSection(IdentityStorageApiOptions.IdentityStorageApi));

    builder.Services.AddAutoMapper(m =>
    {
        m.AddProfile<DataProfile>();
    });

    builder.Services.AddScoped<IDtoValidator, DtoValidator>();
    builder.Services.AddHttpClient<IClaimClient, ClaimClient>();

    var appOptions = builder.Configuration.GetSection(AppOptions.App).Get<AppOptions>() ?? new AppOptions();
    Log.Information($"{appOptions.Name} ver:{appOptions.Version}");

    // Add Authentication Services
    var srvAuthOptions = builder.Configuration.GetSection(ServerAuthenticationOptions.Authentication).Get<ServerAuthenticationOptions>()
        ?? new ServerAuthenticationOptions();
    builder.Services.AddAuthentication(srvAuthOptions.Scheme)
        .AddScheme<AuthenticationSchemeOptions, ServerFixedTokenAuthenticationHandler>(srvAuthOptions.Scheme, options =>
        {
            options.TimeProvider = TimeProvider.System;
        });
    builder.Services.AddAuthorization();

    // MCP Server Setup
    builder.Services
        .AddMcpServer(options =>
        {
            options.ServerInfo = new() { Name = appOptions.Name, Version = appOptions.Version };
            options.ServerInstructions = "Manage identity claims.";
        })
        .WithHttpTransport()
        .WithTools([typeof(ClaimTools), typeof(AppInfoTool)]);

    // 
    var webapp = builder.Build();
    webapp.UseAuthentication();
    webapp.UseAuthorization();
    webapp.MapMcp(appOptions.MapMcpPattern)
        .RequireAuthorization(); // Protect the MCP endpoint

    await webapp.RunAsync(appOptions.Url);
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