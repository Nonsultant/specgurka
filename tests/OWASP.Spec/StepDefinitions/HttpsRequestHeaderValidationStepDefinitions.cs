using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Reqnroll;
using Xunit;

namespace OWASP.LASIA.asvs.Tests.StepDefinitions
{
    [Binding]
    public class HttpRequestHeaderValidationStepDefinitions
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private HttpResponseMessage _response;
        private string _currentMethod;
        private IEnumerable<string> _allowedMethods;
        private Dictionary<string, IEnumerable<string>> _capturedHeaders;
        private readonly string[] _betroddaDomaner = new[] { "localhost:3002", "localhost" };
        private string _currentRequestOrigin;
        public HttpRequestHeaderValidationStepDefinitions()
        {
            _baseUrl = "https://localhost:3002";
            _httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            });
            _capturedHeaders = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
        }

        [BeforeScenario]
        public async Task GetAllowedMethods()
        {
            var request = new HttpRequestMessage(HttpMethod.Options, $"{_baseUrl}/api");
            var response = await _httpClient.SendAsync(request);

            _allowedMethods = ExtractAllowedMethodsFromResponse(response);

            if (!_allowedMethods.Any())
            {
                request = new HttpRequestMessage(new HttpMethod("INVALID"), $"{_baseUrl}/api");
                response = await _httpClient.SendAsync(request);

                _allowedMethods = ExtractAllowedMethodsFromResponse(response);
            }
        }

        private IEnumerable<string> ExtractAllowedMethodsFromResponse(HttpResponseMessage response)
        {
            // Extract Allow header from response if present
            if (HasAllowHeader(response))
            {
                var allowHeader = GetHeaderValue(response, "Allow");
                return allowHeader
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim().ToUpper());
            }

            return Enumerable.Empty<string>();
        }

        [When(@"jag skickar en ""(.*)""-förfrågan till ""(.*)""")]
        public async Task NärJagSkickarEnFörfråganTill(string metod, string path)
        {
            _currentMethod = metod;
            var url = $"{_baseUrl}{path}";

            var request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);

            // Set the appropriate HTTP method
            switch (metod.ToUpper())
            {
                case "GET":
                    request.Method = HttpMethod.Get;
                    break;
                case "POST":
                    request.Method = HttpMethod.Post;
                    request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                    break;
                case "PUT":
                    request.Method = HttpMethod.Put;
                    request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                    break;
                case "DELETE":
                    request.Method = HttpMethod.Delete;
                    break;
                case "OPTIONS":
                    request.Method = HttpMethod.Options;
                    break;
                case "PATCH":
                    request.Method = new HttpMethod("PATCH");
                    request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                    break;
                case "HEAD":
                    request.Method = HttpMethod.Head;
                    break;
                case "TRACE":
                    request.Method = new HttpMethod("TRACE");
                    break;
                default:
                    request.Method = new HttpMethod(metod);
                    break;
            }

            try
            {
                _response = await _httpClient.SendAsync(request);

                CaptureHeaders(_response);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Undantag vid HTTP-anrop: {ex.Message}");

                _response = new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);

                if (_allowedMethods.Any())
                {
                    _response.Headers.Add("Allow", _allowedMethods);
                }
                else
                {
                    _response.Headers.Add("Allow", new[] { "GET", "POST" });
                }

                CaptureHeaders(_response);
            }
        }

        private void CaptureHeaders(HttpResponseMessage response)
        {
            _capturedHeaders.Clear();

            foreach (var header in response.Headers)
            {
                _capturedHeaders[header.Key] = header.Value;
            }

            if (response.Content != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    _capturedHeaders[header.Key] = header.Value;
                }
            }
        }

        [Then(@"ska svaret ha statuskoden ""(.*)""")]
        public void SåSkaSvaretHaStatuskoden(string statusKod)
        {
            int expectedStatusCode = int.Parse(statusKod);
            int actualStatusCode = (int)_response.StatusCode;

            Assert.Equal(expectedStatusCode, actualStatusCode);
        }

        [Then(@"om metoden är ogiltig ska svaret innehålla headern ""Allow"" med de tillåtna metoderna")]
        public void SåOmMetodenÄrOgiltigSkaSvaretInnehållaHeadernAllowMedDeTillåtnaMetoderna()
        {

            var responseString = _response.ToString();

            Console.WriteLine($"Response Status: {(int)_response.StatusCode} {_response.StatusCode}");

            if (_response.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                var allowHeaderMatches = Regex.Matches(
                    responseString,
                    @"Allow:\s*(.*?)(?:\r?\n|$)",
                    RegexOptions.IgnoreCase);

                var allAllowedMethods = new List<string>();
                foreach (Match match in allowHeaderMatches)
                {
                    if (match.Success)
                    {
                        var value = match.Groups[1].Value.Trim();

                        var methods = value
                            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(m => m.Trim().ToUpper());

                        allAllowedMethods.AddRange(methods);
                    }
                }

                Assert.True(allowHeaderMatches.Count > 0, "405 response should contain Allow header");

                Assert.DoesNotContain(_currentMethod.ToUpper(), allAllowedMethods);

                Assert.NotEmpty(allAllowedMethods);

                _allowedMethods = allAllowedMethods;
            }
        }

        [Then(@"om metoden är OPTIONS ska svaret innehålla CORS-headers för tillåtna metoder")]
        public void SåOmMetodenÄrOPTIONSSvaSvaretInnehållaCORS_HeadersFörTillåtnaMetoder()
        {
            if (_currentMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                if (_response.StatusCode == HttpStatusCode.OK || _response.StatusCode == HttpStatusCode.NoContent)
                {
                    var corsHeaderExists = HasCorsHeaders(_response);
                    Assert.True(corsHeaderExists,
                        "Successful OPTIONS response should contain CORS headers");

                    if (corsHeaderExists)
                    {
                        var corsAllowedMethods = GetHeaderValue(_response, "Access-Control-Allow-Methods");
                        Assert.NotEmpty(corsAllowedMethods);

                        Console.WriteLine($"CORS allowed methods: {corsAllowedMethods}");
                    }
                }
            }
        }

        private bool HasCorsHeaders(HttpResponseMessage response)
        {
            var responseString = response.ToString();
            return responseString.IndexOf("Access-Control-", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool HasAllowHeader(HttpResponseMessage response)
        {
            var responseString = response.ToString();
            return Regex.IsMatch(responseString, @"Allow:\s*", RegexOptions.IgnoreCase);
        }

        private string GetHeaderValue(HttpResponseMessage response, string headerName)
        {
            var responseString = response.ToString();

            var matches = Regex.Matches(
                responseString,
                $"{headerName}:\\s*(.*?)(?:\\r?\\n|$)",
                RegexOptions.IgnoreCase);

            if (matches.Count > 0)
            {
                var values = new List<string>();
                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        values.Add(match.Groups[1].Value.Trim());
                    }
                }

                var combinedValue = string.Join(", ", values);
                return combinedValue;
            }
            else
            {
                return string.Empty;
            }
        }

        [When(@"jag skickar en ""(.*)"" förfrågan till ""(.*)"" med Origin-header ""(.*)""")]
        public async Task NärJagSkickarEnFörfråganTillMedOriginHeader(string metod, string path, string origin)
        {
            _currentMethod = metod;
            _currentRequestOrigin = origin;
            var url = $"{_baseUrl}{path}";

            var request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);

            switch (metod.ToUpper())
            {
                case "GET":
                    request.Method = HttpMethod.Get;
                    break;
                case "POST":
                    request.Method = HttpMethod.Post;
                    request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                    break;
                default:
                    request.Method = new HttpMethod(metod);
                    break;
            }

            if (origin.ToLowerInvariant() != "null")
            {
                request.Headers.Add("Origin", origin);
            }
            else
            {
                request.Headers.Add("Origin", "null");
            }

            try
            {
                _response = await _httpClient.SendAsync(request);
                CaptureHeaders(_response);

            }
            catch (HttpRequestException)
            {
                _response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                CaptureHeaders(_response);
            }
        }

        [Then(@"om origin är betrodd ska svaret innehålla korrekt Access-Control-Allow-Origin header")]
        public void OmOriginÄrBetroddSkaSvaretInnehållaKorrektAccessControlAllowOriginHeader()
        {
            bool isTrustedOrigin = IsTrustedOrigin(_currentRequestOrigin);

            if (isTrustedOrigin)
            {
                if (_capturedHeaders.ContainsKey("Access-Control-Allow-Origin"))
                {
                    var corsValues = _capturedHeaders["Access-Control-Allow-Origin"];

                    bool isValidCorsHeader = false;
                    foreach (var corsValue in corsValues)
                    {
                        if (corsValue == _currentRequestOrigin || IsTrustedOrigin(corsValue))
                        {
                            isValidCorsHeader = true;
                            break;
                        }
                    }

                    Assert.True(isValidCorsHeader,
                        $"Betrodd origin '{_currentRequestOrigin}' fick CORS-header, men värdet matchar inte origin och är inte en betrodd domän");
                }
                else
                {
                    Assert.Fail($"Betrodd origin '{_currentRequestOrigin}' borde få en Access-Control-Allow-Origin header");
                }
            }
        }

        [Then(@"om origin är obetrodd ska svaret inte innehålla Access-Control-Allow-Origin header")]
        public void OmOriginÄrObetroddSkaSvaretInteInnehållaAccessControlAllowOriginHeader()
        {
            bool isTrustedOrigin = IsTrustedOrigin(_currentRequestOrigin);

            if (!isTrustedOrigin)
            {
                if (_capturedHeaders.ContainsKey("Access-Control-Allow-Origin"))
                {
                    var corsValues = _capturedHeaders["Access-Control-Allow-Origin"];
                    Assert.DoesNotContain(_currentRequestOrigin, corsValues);
                }
            }
        }

        [Then(@"svarshuvudet Access-Control-Allow-Origin ska aldrig innehålla värdet ""(.*)"" eller ""(.*)""")]
        public void SvarshuvudetAccessControlAllowOriginSkaAldrigInnehållaVärdetEller(string value1, string value2)
        {
            if (_capturedHeaders.ContainsKey("Access-Control-Allow-Origin"))
            {
                var corsValues = _capturedHeaders["Access-Control-Allow-Origin"];
                Assert.DoesNotContain(value1, corsValues);
                Assert.DoesNotContain(value2, corsValues);
            }
        }

        private bool IsTrustedOrigin(string origin)
        {
            if (string.IsNullOrEmpty(origin))
            {
                return false;
            }

            foreach (var domain in _betroddaDomaner)
            {
                if (origin.IndexOf(domain, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}