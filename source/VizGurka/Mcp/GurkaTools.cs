using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using SpecGurka.GurkaSpec;
using VizGurka.Helpers;
using VizGurka.Services;

namespace VizGurka.Mcp;

[McpServerToolType]
public class GurkaTools
{
    private readonly SearchService _searchService;
    private readonly QueryMapperHelper _queryMapperHelper;
    private readonly ProductNameHelper _productNameHelper;

    public GurkaTools(SearchService searchService, QueryMapperHelper queryMapperHelper, ProductNameHelper productNameHelper)
    {
        _searchService = searchService;
        _queryMapperHelper = queryMapperHelper;
        _productNameHelper = productNameHelper;
    }

    [McpServerTool(Name = "list_products", ReadOnly = true, Idempotent = true)]
    [Description("Lists all products that have BDD test results in VizGurka, with a summary of their latest test run.")]
    public List<ProductSummary> ListProducts()
    {
        return TestrunReader.GetUniqueProductNames()
            .Select(name =>
            {
                var run = TestrunReader.ReadLatestRun(name);
                return new ProductSummary(
                    name,
                    _productNameHelper.GetPrettyProductName(name),
                    run?.RunDate,
                    run is null ? null : FindProduct(run, name).TestsPassed,
                    run?.BranchName,
                    run?.CommitId);
            })
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    [McpServerTool(Name = "get_latest_testrun", ReadOnly = true, Idempotent = true)]
    [Description("Gets the latest test run for a product: commit info, overall result, status counts and a status summary for every feature. Use the feature ids with get_feature.")]
    public TestrunSummary GetLatestTestrun(
        [Description("Product name as returned by list_products.")] string productName)
    {
        var (run, product) = ReadLatest(productName);
        var scenarios = product.Features.SelectMany(AllScenarios).ToList();

        return new TestrunSummary(
            product.Name,
            run.RunDate,
            product.TestsPassed,
            product.TestDuration,
            run.BranchName,
            run.CommitId,
            run.CommitAuthor,
            run.CommitDate,
            run.CommitMessage,
            run.BaseUrl,
            CountStatuses(product.Features.Select(f => f.Status)),
            CountStatuses(scenarios.Select(s => s.Status)),
            product.Features.Select(f => new FeatureSummary(
                f.Id,
                f.Name,
                f.Status.ToString(),
                f.FilePath,
                f.Tags,
                f.TestDuration,
                CountStatuses(AllScenarios(f).Select(s => s.Status)))).ToList());
    }

    [McpServerTool(Name = "get_feature", ReadOnly = true, Idempotent = true)]
    [Description("Gets the full details of a feature in the latest test run: description, background, rules, scenarios, steps, step results and error messages.")]
    public FeatureDetail GetFeature(
        [Description("Product name as returned by list_products.")] string productName,
        [Description("Feature id (GUID) from get_latest_testrun or search. Alternatively the exact feature name.")] string feature)
    {
        var (_, product) = ReadLatest(productName);

        var match = Guid.TryParse(feature, out var id)
            ? product.Features.FirstOrDefault(f => f.Id == id)
            : product.Features.FirstOrDefault(f => string.Equals(f.Name, feature, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            throw new McpException($"Feature '{feature}' was not found in the latest test run for '{productName}'. Use get_latest_testrun to list features.");
        }

        return new FeatureDetail(
            match.Id,
            match.Name,
            match.Status.ToString(),
            match.FilePath,
            match.Description,
            match.Tags,
            match.TestDuration,
            ToDetail(match.Background),
            match.Scenarios.Select(ToDetail).ToList(),
            match.Rules.Select(r => new RuleDetail(
                r.Name,
                r.Status.ToString(),
                r.Description,
                r.Tags,
                ToDetail(r.Background),
                r.Scenarios.Select(ToDetail).ToList())).ToList());
    }

    [McpServerTool(Name = "get_failing_scenarios", ReadOnly = true, Idempotent = true)]
    [Description("Lists every failed scenario in the latest test run for a product, with the failing step and its error message.")]
    public List<FailingScenario> GetFailingScenarios(
        [Description("Product name as returned by list_products.")] string productName)
    {
        var (_, product) = ReadLatest(productName);
        var result = new List<FailingScenario>();

        foreach (var feature in product.Features)
        {
            var sections = new List<(Rule? Rule, List<Scenario> Scenarios)> { (null, feature.Scenarios) };
            sections.AddRange(feature.Rules.Select(r => ((Rule?)r, r.Scenarios)));

            foreach (var (rule, scenarios) in sections)
            {
                foreach (var scenario in scenarios.Where(s => s.Status == Status.Failed))
                {
                    var failedStep = (feature.Background?.Steps ?? [])
                        .Concat(rule?.Background?.Steps ?? [])
                        .Concat(scenario.Steps)
                        .FirstOrDefault(s => s.Status == Status.Failed);

                    result.Add(new FailingScenario(
                        feature.Id,
                        feature.Name,
                        rule?.Name,
                        scenario.Name,
                        scenario.Tags,
                        failedStep is null ? null : $"{failedStep.Kind} {failedStep.Text}",
                        failedStep?.TestErrorMessage));
                }
            }
        }

        return result;
    }

    [McpServerTool(Name = "search", ReadOnly = true, Idempotent = true)]
    [Description("Full-text search across features, scenarios, steps, tags and test runs. Supports '@tag' queries and field prefixes such as 'feature:', 'scenario:', 'step:', 'tag:', 'rule:', 'feature status:' and 'scenario status:'.")]
    public List<SearchHit> Search(
        [Description("Search query, e.g. 'login', '@smoke' or 'scenario status: Failed'.")] string query,
        [Description("Optional filter: 'features', 'scenarios', 'rules' or 'tags'.")] string? filter = null,
        [Description("Maximum number of results to return (1-50).")] int limit = 20)
    {
        var mappedQuery = _queryMapperHelper.MapQuery(query);

        return _searchService.Search(mappedQuery, filter ?? string.Empty, _queryMapperHelper.GetMappings())
            .Take(Math.Clamp(limit, 1, 50))
            .Select(r => new SearchHit(
                r.DocumentType,
                r.Title,
                r.Content,
                r.Status,
                NullIfEmpty(r.FeatureId),
                NullIfEmpty(r.ParentFeatureId),
                NullIfEmpty(r.ParentFeatureName),
                r.Tags,
                r.FileName,
                r.Score))
            .ToList();
    }

    private static (Testrun Run, Product Product) ReadLatest(string productName)
    {
        var run = TestrunReader.ReadLatestRun(productName)
            ?? throw new McpException($"No test run found for product '{productName}'. Use list_products to see available products.");

        return (run, FindProduct(run, productName));
    }

    private static Product FindProduct(Testrun run, string productName) =>
        run.Products.FirstOrDefault(p => string.Equals(p.Name, productName, StringComparison.OrdinalIgnoreCase))
        ?? run.Products.First();

    private static IEnumerable<Scenario> AllScenarios(Feature feature) =>
        feature.Scenarios.Concat(feature.Rules.SelectMany(r => r.Scenarios));

    private static StatusCounts CountStatuses(IEnumerable<Status> statuses)
    {
        var list = statuses.ToList();
        return new StatusCounts(
            list.Count(s => s == Status.Passed),
            list.Count(s => s == Status.Failed),
            list.Count(s => s == Status.NotImplemented));
    }

    private static BackgroundDetail? ToDetail(Background? background) =>
        background is null
            ? null
            : new BackgroundDetail(background.Name, background.Description, background.Steps.Select(ToDetail).ToList());

    private static ScenarioDetail ToDetail(Scenario scenario) =>
        new(
            scenario.Name,
            scenario.Status.ToString(),
            scenario.Description,
            scenario.Tags,
            scenario.TestDuration,
            scenario.IsOutline,
            scenario.Examples,
            scenario.Steps.Select(ToDetail).ToList());

    private static StepDetail ToDetail(Step step) =>
        new(
            step.Kind,
            step.Text,
            step.Status.ToString(),
            step.Table,
            step.TestDurationSeconds,
            step.TestErrorMessage);

    private static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;
}
