namespace VizGurka.Helpers;

public class ProductNameHelper
{
    private readonly IConfiguration _configuration;

    public ProductNameHelper(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetPrettyProductName(string productName)
    {
        var prettyProductNamesSection = _configuration
            .GetSection("TagPatterns:Github:PrettyProductNames")
            .Get<List<Dictionary<string, string>>>();

        if (prettyProductNamesSection != null)
        {
            foreach (var dict in prettyProductNamesSection)
            {
                if (dict.TryGetValue(productName, out var pretty))
                    return pretty;
            }
        }
        
        return productName;
    }
}