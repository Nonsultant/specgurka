using Microsoft.AspNetCore.Localization;

namespace VizGurka.Providers;

public class SwedishCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var acceptLanguageHeader = httpContext.Request.GetTypedHeaders().AcceptLanguage;
        
        if (acceptLanguageHeader == null || acceptLanguageHeader.Count == 0)
            return NullProviderCultureResult;
        
        foreach (var language in acceptLanguageHeader.OrderByDescending(h => h.Quality ?? 1))
        {
            var cultureName = language.Value.ToString();
            if (cultureName.StartsWith("sv", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult("sv-SE", "sv-SE"));
            }
        }

        return NullProviderCultureResult;
    }
}