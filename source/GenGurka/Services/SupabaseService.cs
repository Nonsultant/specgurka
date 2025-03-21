using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GenGurka.Services
{
    public static class SupabaseService
    {
        private static string? _supabaseUrl;
        private static string? _supabaseKey;
        private static string? _supabaseBucket;
        private static string? _supabasePublicKey;
        private static bool _isInitialized = false;
        private static readonly HttpClient _httpClient = new HttpClient();

        public static void Initialize(IConfiguration configuration)
        {
            _supabaseUrl = configuration["Supabase:Url"]?.TrimEnd('/');
            _supabasePublicKey = configuration["Supabase:PublicKey"];
            _supabaseKey = configuration["Supabase:Key"];
            _supabaseBucket = configuration["Supabase:Bucket"];
            _isInitialized = true;

            Console.WriteLine($"Initialized Supabase storage client from configuration:");
            Console.WriteLine($"URL: {_supabaseUrl}");
            Console.WriteLine($"Bucket: {_supabaseBucket}");
        }

        public static async Task<bool> UploadToSupabase(string filePath)
        {
            if (string.IsNullOrEmpty(_supabaseUrl) || string.IsNullOrEmpty(_supabaseKey) || string.IsNullOrEmpty(_supabaseBucket))
            {
                Console.WriteLine("Error: Missing Supabase credentials. Check your configuration.");
                Console.WriteLine($"URL: {_supabaseUrl ?? "missing"}");
                Console.WriteLine($"Key: {(_supabaseKey != null ? "present" : "missing")}");
                Console.WriteLine($"Bucket: {_supabaseBucket ?? "missing"}");
                return false;
            }

            try
            {
                string fileName = Path.GetFileName(filePath);
                string uploadUrl = $"{_supabaseUrl}/storage/v1/object/{_supabaseBucket}/{fileName}";

                Console.WriteLine($"Uploading {filePath} to Supabase: {uploadUrl}");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");

                byte[] fileContent = await File.ReadAllBytesAsync(filePath);

                using var content = new ByteArrayContent(fileContent);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                var response = await _httpClient.PutAsync(uploadUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully uploaded {fileName} to Supabase bucket {_supabaseBucket}");
                    return true;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error uploading to Supabase: {response.StatusCode}");
                    Console.WriteLine($"Error details: {errorContent}");

                    Console.WriteLine("Trying POST method instead...");
                    response = await _httpClient.PostAsync(uploadUrl, new ByteArrayContent(fileContent));

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Successfully uploaded {fileName} using POST method");
                        return true;
                    }
                    else
                    {
                        errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"POST method also failed: {response.StatusCode}");
                        Console.WriteLine($"Error details: {errorContent}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during upload: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }
    }
}