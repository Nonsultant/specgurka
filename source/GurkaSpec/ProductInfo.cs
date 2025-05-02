using GurkaSpec.Helpers;

namespace SpecGurka.GurkaSpec;

public class ProductInfo
{
    public string ProductName { get; set; } = string.Empty;
    public DateTime LatestRunDateUtc { get; set; } = DateTime.MinValue;
    public Guid Id { get; set; } = Guid.Empty;
    public string Culture { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string CommitAuthor { get; set; } = string.Empty;

    public string GetFormattedDateTime()
    {
        return DateTimeHelper.FormatDateTimeForCulture(LatestRunDateUtc, Culture);
    }
}