using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using VizGurka.Helpers;
using VizGurka.Services;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

TestrunReader.Initialize(configuration);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddSingleton<PowerShellService>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddRazorPages()
    .AddViewLocalization();

var supportedCultures = new[] { "en-GB", "sv-SE" };
var defaultCulture = "en-GB";

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(defaultCulture)
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// The order of these middleware is important for localization to work as intended
app.UseRequestLocalization(localizationOptions);
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var powerShellService = serviceProvider.GetRequiredService<PowerShellService>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    // Run in a background task
    Task.Run(async () =>
    {
        try
        {
            logger.LogInformation("Running GitHub artifact fetch at startup");
            var result = await powerShellService.RunScriptAsync();
            if (result.Success)
            {
                logger.LogInformation("GitHub artifact fetch completed successfully");
                // Reinitialize data after script completes
                TestrunReader.Initialize(configuration);
            }
            else
            {
                logger.LogWarning("GitHub artifact fetch completed with issues: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running GitHub artifact fetch at startup");
        }
    });
}

app.Run();