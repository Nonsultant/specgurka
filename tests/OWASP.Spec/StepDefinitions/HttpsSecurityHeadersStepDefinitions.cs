using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Reqnroll;
using Xunit;

namespace OWASP.LASIA.asvs.Tests.StepDefinitions
{
    [Binding]
    public class HttpSecurityHeadersStepDefinitions
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private HttpResponseMessage _response;

        public HttpSecurityHeadersStepDefinitions()
        {
            _baseUrl = "https://localhost:3002";
            _httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            });
        }

        [When(@"jag skickar en GET-förfrågan till ""(.*)""")]
        public async Task NärJagSkickarEnGET_FörfråganTill(string path)
        {
            var url = $"{_baseUrl}{path}";
            _response = await _httpClient.GetAsync(url);
        }

        [Then(@"ska svaret innehålla en Content-Type-rubrik med innehållstyp ""(.*)""")]
        public void SåSkaSvaretInnehållaEnContent_Type_RubrikMedInnehållstyp(string innehållstyp)
        {
            Assert.NotNull(_response.Content.Headers.ContentType);
            Assert.Equal(innehållstyp, _response.Content.Headers.ContentType.MediaType);
        }

        [Then(@"om innehållstypen är av texttyp ska den innehålla en säker teckenuppsättning")]
        public void SåOmInnehållstypenÄrAvTexttypSkaDenInnehållaEnSäkerTeckenuppsättning()
        {
            var contentType = _response.Content.Headers.ContentType;

            if (contentType.MediaType.StartsWith("text/") ||
                contentType.MediaType.EndsWith("+xml") ||
                contentType.MediaType.Equals("application/xml"))
            {
                Assert.NotNull(contentType.CharSet);

                Assert.True(
                    contentType.CharSet.Equals("utf-8", StringComparison.OrdinalIgnoreCase) ||
                    contentType.CharSet.Equals("iso-8859-1", StringComparison.OrdinalIgnoreCase) ||
                    contentType.CharSet.Equals("windows-1252", StringComparison.OrdinalIgnoreCase),
                    $"Osäker eller ovanlig teckenuppsättning: {contentType.CharSet}"
                );
            }
        }
        [Then(@"ska svaret innehålla en Content-Security-Policy-rubrik")]
        public void SåSkaSvaretInnehållaEnContent_Security_Policy_Rubrik()
        {
            bool hasCspHeader = _response.Headers.Contains("Content-Security-Policy");
            Assert.True(hasCspHeader, "Svaret innehåller inte en Content-Security-Policy-rubrik");
        }

        [Then(@"ska CSP-rubriken innehålla restriktioner för ""(.*)""")]
        public void SåCSP_RubrikenSkaInnehållaRestriktionerFör(string directive)
        {
            string cspHeader = _response.Headers.GetValues("Content-Security-Policy").FirstOrDefault();
            Assert.NotNull(cspHeader);

            var directivePattern = new Regex($@"{directive}\s+([^;]+)");
            var match = directivePattern.Match(cspHeader);

            Assert.True(match.Success, $"CSP-rubriken innehåller inte direktivet '{directive}'");
            Assert.False(string.IsNullOrWhiteSpace(match.Groups[1].Value),
                $"CSP-direktivet '{directive}' har inget värde");
        }

        [Then(@"ska CSP-rubriken inte innehålla ""(.*)"" för script")]
        public void SåCSP_RubrikenSkaInteInnehållaFörScript(string unsafeValue)
        {
            string cspHeader = _response.Headers.GetValues("Content-Security-Policy").FirstOrDefault();
            Assert.NotNull(cspHeader);

            string directiveValue = GetDirectiveValue(cspHeader, "script-src") ??
                                    GetDirectiveValue(cspHeader, "default-src");

            Assert.NotNull(directiveValue);

            Assert.DoesNotContain(unsafeValue, directiveValue, StringComparison.OrdinalIgnoreCase);
        }

        [Then(@"ska CSP-rubriken innehålla en rapporteringsmekanism")]
        public void SåCSP_RubrikenSkaInnehållaEnRapporteringsmekanism()
        {
            bool hasReportOnlyHeader = _response.Headers.Contains("Content-Security-Policy-Report-Only");

            string cspHeader = _response.Headers.GetValues("Content-Security-Policy").FirstOrDefault();
            bool hasReportDirective = false;

            if (cspHeader != null)
            {
                hasReportDirective = cspHeader.Contains("report-uri") || cspHeader.Contains("report-to");
            }

            Assert.True(hasReportOnlyHeader || hasReportDirective,
                "Svaret innehåller ingen CSP-rapporteringsmekanism (report-uri, report-to, eller Content-Security-Policy-Report-Only)");
        }

        [Then(@"ska minst ett av följande vara sant i CSP-rubriken för script:")]
        public void SåSkaMinistEttAvFöljandeVaraSantICSP_RubrikenFörScript(Table table)
        {
            string cspHeader = _response.Headers.GetValues("Content-Security-Policy").FirstOrDefault();
            Assert.NotNull(cspHeader);

            string directiveValue = GetDirectiveValue(cspHeader, "script-src") ??
                                    GetDirectiveValue(cspHeader, "default-src");

            Assert.NotNull(directiveValue);

            bool hasValidOption = false;
            var alternativ = table.CreateSet<ScriptSecurityOption>();

            foreach (var option in alternativ)
            {
                switch (option.Alternativ)
                {
                    case "Innehåller nonce-värden":
                        if (Regex.IsMatch(directiveValue, @"'nonce-[A-Za-z0-9+/=]+'"))
                        {
                            hasValidOption = true;
                        }
                        break;

                    case "Innehåller hash-värden":
                        if (Regex.IsMatch(directiveValue, @"'(sha256|sha384|sha512)-[A-Za-z0-9+/=]+'"))
                        {
                            hasValidOption = true;
                        }
                        break;

                    case "Inga unsafe-inline-värden":
                        if (!directiveValue.Contains("unsafe-inline"))
                        {
                            hasValidOption = true;
                        }
                        break;
                }

                if (hasValidOption) break;
            }

            Assert.True(hasValidOption,
                "CSP-rubriken uppfyller inte något av de säkerhetsalternativen för script: innehåller nonce, innehåller hash eller har inga unsafe-inline värden");
        }

        private class ScriptSecurityOption
        {
            public string Alternativ { get; set; }
        }

        private string GetDirectiveValue(string cspHeader, string directive)
        {
            var match = Regex.Match(cspHeader, $@"{directive}\s+([^;]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        [Then(@"ska svaret innehålla en X-Content-Type-Options-rubrik")]
        public void SåSkaSvaretInnehållaEnX_Content_Type_Options_Rubrik()
        {
            bool hasHeader = _response.Headers.Contains("X-Content-Type-Options");
            Assert.True(hasHeader, "Svaret innehåller inte en X-Content-Type-Options-rubrik");
        }

        [Then(@"X-Content-Type-Options-rubriken ska vara satt till ""(.*)""")]
        public void SåX_Content_Type_Options_RubrikenSkaVaraSattTill(string expectedValue)
        {
            Assert.True(_response.Headers.Contains("X-Content-Type-Options"),
                "Svaret innehåller inte en X-Content-Type-Options-rubrik");

            var headerValue = _response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault();

            Assert.Equal(expectedValue, headerValue, StringComparer.OrdinalIgnoreCase);
        }
        [Then(@"ska svaret innehålla en Referrer-Policy-rubrik")]
        public void SåSkaSvaretInnehållaEnReferrer_Policy_Rubrik()
        {
            bool hasHeader = _response.Headers.Contains("Referrer-Policy");
            Assert.True(hasHeader, "Svaret innehåller inte en Referrer-Policy-rubrik");
        }

        [Then(@"Referrer-Policy-rubriken ska ha något av följande säkra värden:")]
        public void SåReferrer_Policy_RubrikenSkaHaNågotAvFöljandeSäkraVärden(Table table)
        {
            Assert.True(_response.Headers.Contains("Referrer-Policy"),
                "Svaret innehåller inte en Referrer-Policy-rubrik");

            var headerValue = _response.Headers.GetValues("Referrer-Policy").FirstOrDefault();
            Assert.NotNull(headerValue);

            var safeValues = new List<string>();
            foreach (var row in table.Rows)
            {
                safeValues.Add(row[0].ToLowerInvariant());
            }

            var actualPolicies = headerValue.Split(',')
                .Select(v => v.Trim().ToLowerInvariant())
                .ToList();

            bool hasAnySecureValue = actualPolicies.Any(policy => safeValues.Contains(policy));

            Assert.True(hasAnySecureValue,
                $"Referrer-Policy-rubriken '{headerValue}' innehåller inget av de säkra värdena: {string.Join(", ", safeValues)}");
        }

        [Then(@"Referrer-Policy-rubriken ska inte innehålla något av följande osäkra värden:")]
        public void SåReferrer_Policy_RubrikenSkaInteInnehållaNågotAvFöljandeOsäkraVärden(Table table)
        {
            Assert.True(_response.Headers.Contains("Referrer-Policy"),
                "Svaret innehåller inte en Referrer-Policy-rubrik");

            var headerValue = _response.Headers.GetValues("Referrer-Policy").FirstOrDefault();
            Assert.NotNull(headerValue);

            var unsafeValues = new List<string>();
            foreach (var row in table.Rows)
            {
                unsafeValues.Add(row[0].ToLowerInvariant());
            }

            var actualPolicies = headerValue.Split(',')
                .Select(v => v.Trim().ToLowerInvariant())
                .ToList();

            var foundUnsafeValues = actualPolicies.Where(policy => unsafeValues.Contains(policy)).ToList();

            Assert.True(!foundUnsafeValues.Any(),
                $"Referrer-Policy-rubriken '{headerValue}' innehåller följande osäkra värden: {string.Join(", ", foundUnsafeValues)}");
        }

        [Then(@"ska svaret innehålla en Strict-Transport-Security-rubrik")]
        public void SåSkaSvaretInnehållaEnStrict_Transport_Security_Rubrik()
        {
            bool hasHeader = _response.Headers.Contains("Strict-Transport-Security");
            Assert.True(hasHeader, "Svaret innehåller inte en Strict-Transport-Security-rubrik");
        }

        [Then(@"Strict-Transport-Security-rubriken ska innehålla ""max-age"" med värde minst (\d+)")]
        public void SåStrict_Transport_Security_RubrikenSkaInnehållaMaxAgeMedVärdeMinst(long minExpectedMaxAge)
        {
            Assert.True(_response.Headers.Contains("Strict-Transport-Security"),
                "Svaret innehåller inte en Strict-Transport-Security-rubrik");

            var headerValue = _response.Headers.GetValues("Strict-Transport-Security").FirstOrDefault();
            Assert.NotNull(headerValue);

            var maxAgePattern = new Regex(@"max-age=(\d+)");
            var match = maxAgePattern.Match(headerValue);

            Assert.True(match.Success, $"Strict-Transport-Security-rubriken '{headerValue}' innehåller inget max-age-direktiv");

            long actualMaxAge = long.Parse(match.Groups[1].Value);
            Assert.True(actualMaxAge >= minExpectedMaxAge,
                $"max-age-värdet ({actualMaxAge}) i Strict-Transport-Security-rubriken är mindre än det förväntade minimivärdet ({minExpectedMaxAge})");
        }

        [Then(@"Strict-Transport-Security-rubriken ska inkludera ""includeSubdomains""")]
        public void SåStrict_Transport_Security_RubrikenSkaInkluderaIncludeSubdomains()
        {
            Assert.True(_response.Headers.Contains("Strict-Transport-Security"),
                "Svaret innehåller inte en Strict-Transport-Security-rubrik");

            var headerValue = _response.Headers.GetValues("Strict-Transport-Security").FirstOrDefault();
            Assert.NotNull(headerValue);

            bool containsIncludeSubdomains = headerValue.IndexOf("includeSubdomains", StringComparison.OrdinalIgnoreCase) >= 0;

            Assert.True(containsIncludeSubdomains,
                $"Strict-Transport-Security-rubriken '{headerValue}' innehåller inte 'includeSubdomains'-direktivet");
        }

        [Then(@"ska Strict-Transport-Security-rubriken inkludera ""preload""")]
        public void SåSkaStrict_Transport_Security_RubrikenInkluderaPreload()
        {
            Assert.True(_response.Headers.Contains("Strict-Transport-Security"),
                "Svaret innehåller inte en Strict-Transport-Security-rubrik");

            var headerValue = _response.Headers.GetValues("Strict-Transport-Security").FirstOrDefault();
            Assert.NotNull(headerValue);

            bool containsPreload = headerValue.IndexOf("preload", StringComparison.OrdinalIgnoreCase) >= 0;

            Assert.True(containsPreload,
                $"Strict-Transport-Security-rubriken '{headerValue}' innehåller inte 'preload'-direktivet");
        }
    }
}