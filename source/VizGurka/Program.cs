using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using VizGurka.Helpers;
using VizGurka.Services;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using VizGurka.Providers;


var configuration = new ConfigurationBuilder()
    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

TestrunReader.Initialize(configuration);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddLogging();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddSingleton<PowerShellService>();
builder.Services.AddSingleton<LuceneIndexService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<QueryMapperHelper>();
builder.Services.AddScoped<MarkdownHelper>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<FeatureFileRepositorySettings>(
    builder.Configuration.GetSection("FeatureFileRepository"));

builder.Services.Configure<TagPatternsSettings>(
    builder.Configuration.GetSection("TagPatterns"));


builder.Services.AddRazorPages()
    .AddViewLocalization();

var app = builder.Build();

var supportedCultures = new[] { "en-GB", "sv-SE" };
var defaultCulture = "en-GB";

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(defaultCulture)
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

localizationOptions.RequestCultureProviders.Clear();
localizationOptions.RequestCultureProviders.Add(new SwedishCultureProvider());

app.UseRequestLocalization(localizationOptions);

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
    var luceneIndexService = serviceProvider.GetRequiredService<LuceneIndexService>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
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

                try
                {
                    logger.LogInformation("Starting LuceneIndexService to index files");
                    luceneIndexService.IndexDirectory();
                    logger.LogInformation("LuceneIndexService indexing completed successfully");

                    // UNCOMMENT THIS TO PRINT THE LUCENE INDEX UPON START
                    //PrintLuceneIndex(luceneIndexService.GetIndexDirectory());
                }
                catch
                {
                    logger.LogWarning("LuceneIndexService indexing failed");
                }
            }
            else
            {
                logger.LogWarning("GitHub artifact fetch completed with issues: {Error}", result.Error);
                
                try
                {
                    logger.LogInformation("Starting LuceneIndexService to index files");
                    luceneIndexService.IndexDirectory();
                    logger.LogInformation("LuceneIndexService indexing completed successfully");

                    // UNCOMMENT THIS TO PRINT THE LUCENE INDEX UPON START
                    //PrintLuceneIndex(luceneIndexService.GetIndexDirectory());
                }
                catch
                {
                    logger.LogWarning("LuceneIndexService indexing failed");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running GitHub artifact fetch at startup");
        }
    });
}

app.Run();


// UNCOMMENT THIS TO PRINT THE LUCENE INDEX UPON START
/* void PrintLuceneIndex(RAMDirectory indexDirectory)
{
    try
    {
        using var indexReader = DirectoryReader.Open(indexDirectory);

        Console.WriteLine($"Number of documents in index: {indexReader.NumDocs}");

        for (int i = 0; i < indexReader.MaxDoc; i++)
        {
            // Get the document
            var document = indexReader.Document(i);

            Console.WriteLine($"Document {i + 1}:");

            // Iterate through all fields in the document
            foreach (var field in document.Fields)
            {
                Console.WriteLine($"\tField Name: {field.Name}");
                Console.WriteLine($"\tField Value: {field.GetStringValue()}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error while printing the Lucene index: {ex.Message}");
    }
} */