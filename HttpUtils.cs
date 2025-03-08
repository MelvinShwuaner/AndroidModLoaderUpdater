using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NeoModLoader.AutoUpdate;

/// <summary>
/// This class is made as utility to make http request easier. Maybe not, just for myself --inmny.
/// </summary>
public static class HttpUtils
{
    private static LoadingScreen loading_screen;

    public static async Task DownloadFile(string url, string file_path)
    {
        var client = new HttpClient();
        using HttpResponseMessage response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
            .ConfigureAwait(false)
            .GetAwaiter().GetResult();
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            return;
        }

        HttpContent content = response.Content;
        if (content == null) throw new Exception("No content in response");

        HttpContentHeaders headers = content.Headers;
        var content_length = headers.ContentLength;
        using var response_stream = content.ReadAsStreamAsync();

        var dir = Path.GetDirectoryName(file_path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        using var file_stream = new FileStream(file_path, FileMode.Create);

        var buffer = new byte[4096];
        int bytesRead;

        ulong total_bytes = 0;
        ulong received_bytes = 0;
        if (headers.ContentLength.HasValue) total_bytes = (ulong)content_length.Value;

        log_progress(received_bytes, total_bytes);

        var last_time = DateTime.Now.Second;
        while (true)
        {
            bytesRead = await response_stream.Result.ReadAsync(buffer, 0, buffer.Length);

            var now = DateTime.Now.Second;
            if (bytesRead == 0)
            {
                if (now - last_time > 3) break;

                continue;
            }

            await file_stream.WriteAsync(buffer, 0, bytesRead);

            received_bytes += (ulong)bytesRead;
            now = DateTime.Now.Second;
            if (now != last_time)
            {
                log_progress(received_bytes, total_bytes);
                last_time = now;
            }
        }

        void log_progress(ulong received, ulong total)
        {
            if (loading_screen == null)
            {
                loading_screen = Object.FindObjectOfType<LoadingScreen>();
                if (loading_screen == null) Debug.Log("Failed to find loading screen.");
            }

            var msg = $"Downloading latest {Path.GetFileName(file_path)}: {received}/{total} bytes";
            if (LocalizedTextManager.instance.language == "cz" || LocalizedTextManager.instance.language == "ch")
                msg = $"正在下载最新 {Path.GetFileName(file_path)}: {received}/{total} B";

            if (loading_screen != null)
            {
                loading_screen.loadingHelperText.text = msg;
                Debug.Log(msg);
            }

            if (loading_screen?.inGameScreen ?? false) WorldTip.showNow(msg, false, "top");
        }
    }

    public static string Request(string url, string param = "", string method = "get")
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; //TLS1.2=3702

        string result = "";
        HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
        if (req == null) return result;
        req.Method = method;
        req.ContentType = @"application/octet-stream";
        req.UserAgent =
            @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36";
        byte[] post_data = Encoding.GetEncoding("UTF-8").GetBytes(param);
        HttpWebResponse res;
        if (post_data.Length > 0)
        {
            req.ContentLength = post_data.Length;
            req.Timeout = 15000;
            Stream output_stream = req.GetRequestStream();
            output_stream.WriteAsync(post_data, 0, post_data.Length);
            output_stream.FlushAsync();
            output_stream.Close();
            try
            {
                res = (HttpWebResponse)req.GetResponse();
                Stream input_stream = res.GetResponseStream();
                Encoding encoding = Encoding.GetEncoding("UTF-8");
                StreamReader sr = new(input_stream, encoding);
                result = sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return result;
            }
        }
        else
        {
            try
            {
                res = (HttpWebResponse)req.GetResponse();
                Stream input_stream = res.GetResponseStream();
                Encoding encoding = Encoding.GetEncoding("UTF-8");
                StreamReader sr = new(input_stream, encoding);
                result = sr.ReadToEnd();
                sr.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return result;
            }
        }

        return result;
    }
}