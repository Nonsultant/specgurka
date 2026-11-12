namespace VizGurka.Mcp;

public record ProductSummary(
    string Name,
    string DisplayName,
    string? RunDate,
    bool? TestsPassed,
    string? BranchName,
    string? CommitId);

public record TestrunSummary(
    string Product,
    string RunDate,
    bool TestsPassed,
    string TotalDuration,
    string BranchName,
    string CommitId,
    string CommitAuthor,
    string CommitDate,
    string CommitMessage,
    string RepositoryUrl,
    StatusCounts FeatureCounts,
    StatusCounts ScenarioCounts,
    List<FeatureSummary> Features);

public record StatusCounts(int Passed, int Failed, int NotImplemented);

public record FeatureSummary(
    Guid Id,
    string Name,
    string Status,
    string FilePath,
    List<string> Tags,
    string TestDuration,
    StatusCounts ScenarioCounts);

public record FeatureDetail(
    Guid Id,
    string Name,
    string Status,
    string FilePath,
    string? Description,
    List<string> Tags,
    string TestDuration,
    BackgroundDetail? Background,
    List<ScenarioDetail> Scenarios,
    List<RuleDetail> Rules);

public record RuleDetail(
    string Name,
    string Status,
    string? Description,
    List<string> Tags,
    BackgroundDetail? Background,
    List<ScenarioDetail> Scenarios);

public record BackgroundDetail(
    string? Name,
    string? Description,
    List<StepDetail> Steps);

public record ScenarioDetail(
    string Name,
    string Status,
    string? Description,
    List<string> Tags,
    string TestDuration,
    bool IsOutline,
    string? Examples,
    List<StepDetail> Steps);

public record StepDetail(
    string Kind,
    string Text,
    string Status,
    string? Table,
    string? DurationSeconds,
    string? ErrorMessage);

public record FailingScenario(
    Guid FeatureId,
    string FeatureName,
    string? RuleName,
    string ScenarioName,
    List<string> Tags,
    string? FailedStep,
    string? ErrorMessage);

public record SearchHit(
    string DocumentType,
    string Title,
    string Content,
    string Status,
    string? FeatureId,
    string? ParentFeatureId,
    string? ParentFeatureName,
    List<string> Tags,
    string FileName,
    float Score);
