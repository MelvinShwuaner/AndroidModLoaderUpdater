using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;
using static NeoModLoader.AutoUpdate.UpdateHelper;
namespace NeoModLoader.AutoUpdate;

/// <summary>
/// This class is made as utility to make http request easier. Maybe not, just for myself --inmny.
/// </summary>
public static class HttpUtils
{
    private static LoadingScreen loading_screen;

    public static async Task DownloadFile(string url, string file_path)
{
    try
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        
        response.EnsureSuccessStatusCode();

        var content = response.Content;
        if (content == null) throw new Exception("No content in response");

        var totalBytes = content.Headers.ContentLength ?? 0;
        var receivedBytes = 0ul;

        var dir = Path.GetDirectoryName(file_path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var fileStream = new FileStream(file_path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        using var stream = await content.ReadAsStreamAsync();

        var buffer = new byte[8192];
        int bytesRead;
        var lastProgressUpdate = DateTime.UtcNow;

        log_progress(receivedBytes, totalBytes);

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            receivedBytes += (ulong)bytesRead;

            // Update progress max once per second
            if ((DateTime.UtcNow - lastProgressUpdate).TotalSeconds >= 1)
            {
                log_progress(receivedBytes, totalBytes);
                lastProgressUpdate = DateTime.UtcNow;
            }
        }

        // Final update
        log_progress(receivedBytes, totalBytes);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
        LogMsg("Download failed: 403 Forbidden - Check User-Agent or rate limit");
    }
    catch (Exception ex)
    {
        LogMsg($"Download failed: {ex}");
    }

    // Local progress function
    void log_progress(ulong received, long total)
    {
        string msg;
        string filename = Path.GetFileName(file_path);

        if (LocalizedTextManager.instance.language == "cz" || LocalizedTextManager.instance.language == "ch")
            msg = $"正在下载最新 {filename}: {received}/{total} B";
        else
            msg = $"Downloading latest {filename}: {received}/{total} bytes";
        MelonLogger.Msg(msg);
    }
    }
    private static readonly HttpClient _httpClient = new (new SocketsHttpHandler
    {
        SslOptions = new System.Net.Security.SslClientAuthenticationOptions
        {
            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | 
                                  System.Security.Authentication.SslProtocols.Tls13,

            RemoteCertificateValidationCallback = (message, cert, chain, errors) =>
            {
                if (errors == System.Net.Security.SslPolicyErrors.None) return true;
                if (errors.HasFlag(System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors))
                {
                    MelonLogger.Msg("encountered an SSL error");
                    return true;
                }
                return false;
            }
        }
    })
    {
        // ← This is the important part for GitHub
        DefaultRequestHeaders =
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36" }
            // Or better: use your app name, e.g. "MyApp/1.0"
        }
    };
    public static async Task<string> RequestAsync(string url, string param = "", string method = "GET")
    {
        try
        {
            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                return await _httpClient.GetStringAsync(url);
            }
            else // POST
            {
                var content = new StringContent(param, Encoding.UTF8, "application/json"); 
                // Use "application/octet-stream" if that's what your original code intended

                using var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
            
                return await response.Content.ReadAsStringAsync();
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            LogMsg("GitHub 403 Forbidden - Check rate limit or User-Agent");
            return "";
        }
        catch (Exception ex)
        {
            LogMsg(ex.ToString());
            return "";
        }
    }
    public static string Request(string url, string param = "", string method = "get")
    {
        return RequestAsync(url, param, method).GetAwaiter().GetResult();
    }
}