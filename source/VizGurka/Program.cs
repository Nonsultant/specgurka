using VizGurka.Helpers;
using VizGurka.Services;
using VizGurka.Providers;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
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

builder.Services.AddSingleton<GitHubActionFetcher>();
builder.Services.AddSingleton<ProductNameHelper>();
builder.Services.AddSingleton<LuceneIndexService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<QueryMapperHelper>();
builder.Services.AddScoped<MarkdownHelper>();
builder.Services.AddScoped<VizGurka.Pages.Features.FeaturesModel>();

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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var githubActionFetcher = serviceProvider.GetRequiredService<GitHubActionFetcher>();
    var luceneIndexService = serviceProvider.GetRequiredService<LuceneIndexService>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    // Run GitHub artifact fetch first
    try
    {
        logger.LogInformation("Running GitHub artifact fetch at startup");
        await githubActionFetcher.RunAsync();
        logger.LogInformation("GitHub artifact fetch completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error running GitHub artifact fetch at startup");
    }

    // Run Lucene indexing after GitHub artifact fetch
    try
    {
        logger.LogInformation("Starting LuceneIndexService to index files");
        luceneIndexService.IndexDirectory();
        logger.LogInformation("LuceneIndexService indexing completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "LuceneIndexService indexing failed");
    }
}

app.Run();