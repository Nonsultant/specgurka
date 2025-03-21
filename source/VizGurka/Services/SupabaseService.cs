using Supabase;
using Supabase.Storage;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace VizGurka.Services;

public static class SupabaseService
{
    private static Supabase.Client? _supabase;
    private static IConfiguration? _configuration;

    public static void Initialize(IConfiguration configuration)
    {
        _configuration = configuration;
    }


    public static async Task SyncFilesFromSupabase()
    {

        var supabaseUrl = _configuration["Supabase:Url"]?.TrimEnd('/');
        var supabaseKey = _configuration["Supabase:Key"];
        var bucket = _configuration["Supabase:Bucket"];

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            throw new Exception("Missing Supabase credentials in .env file");

        if (_supabase == null)
        {
            _supabase = new Supabase.Client(supabaseUrl, supabaseKey);
            await _supabase.InitializeAsync();
        }

        var storage = _supabase.Storage.From(bucket);
        var files = await storage.List("");

        string directoryPath = "../VizGurka/GurkaFiles";
        Directory.CreateDirectory(directoryPath);

        foreach (var file in files)
        {
            string fileName = file.Name;

            if (fileName.Equals(".emptyFolderPlaceholder", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var bytes = await storage.Download(fileName, null);
                string localPath = Path.Combine(directoryPath, fileName);

                await File.WriteAllBytesAsync(localPath, bytes);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Failed to write {fileName}: {ex.Message}");
            }
        }
    }
}