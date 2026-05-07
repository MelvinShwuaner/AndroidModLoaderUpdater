using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace NeoModLoader.AutoUpdate;
using static UpdateHelper;
public class GithubUpdater : AUpdater
{
    private const string release_url = "https://api.github.com/repos/MelvinShwuaner/AndroidModLoader/releases/tags/latest";
    private       Version online_version;
    private       ReleaseInfo release_info;

    public override bool CheckUpdate()
    {
        string msg = HttpUtils.Request(release_url);
        release_info = JsonConvert.DeserializeObject<ReleaseInfo>(msg);
        var left_idx = release_info.name.IndexOf('(');
        var right_idx = release_info.name.IndexOf(')');
        online_version = new Version(release_info.name.Substring(left_idx + 1, right_idx - left_idx - 1));
        LogMsg("Github latest version: " + online_version);
        return online_version <= WorldBoxMod.CurrentVersion;
    }

    public override bool IsAvailable()
    {
        return LocalizedTextManager.instance.language != "cz";
    }

    public override async Task<UpdateResult> DownloadAndReplace()
    {
        if (release_info.assets.Length == 0) return UpdateResult.Fail;
        LogMsg("Downloading latest version: " + release_info.name);
        var download_postfix = online_version.ToString().Replace('.', '-');

        using var client = new WebClient();
        ReleaseAsset pdb_asset = release_info.assets.FirstOrDefault(x => x.name.EndsWith(".pdb"));
        if (!string.IsNullOrEmpty(pdb_asset?.browser_download_url))
        {
            var downloaded =
                await UpdateHelper.DownloadFile(pdb_asset.browser_download_url, download_postfix, "NeoModLoader_mobile.pdb");
            if (!string.IsNullOrEmpty(downloaded)) UpdateHelper.TryReplaceFile(Paths.NMLPdbPath, downloaded);
        }

        ReleaseAsset dll_asset = release_info.assets.FirstOrDefault(x => x.name.EndsWith(".dll"));
        if (!string.IsNullOrEmpty(dll_asset?.browser_download_url))
        {
            var NML_taken = !UpdateHelper.BackupFile(Paths.NMLPath);
            var downloaded =
                await UpdateHelper.DownloadFile(dll_asset.browser_download_url, download_postfix, "NeoModLoader_mobile.dll");
            if (NML_taken) return UpdateResult.IsTaken;
            if (!string.IsNullOrEmpty(downloaded))
            {
                if (UpdateHelper.CheckValid(downloaded)) return UpdateHelper.TryReplaceFile(Paths.NMLPath, downloaded);

                try
                {
                    File.Delete(downloaded);
                }
                catch (Exception)
                {
                    // ignored
                }

                UpdateHelper.RestoreFile(Paths.NMLPath);
                UpdateHelper.LoadNMLManually();

                return UpdateResult.Fail;
            }
        }

        return UpdateResult.Fail;
    }

    private class ReleaseInfo
    {
        public ReleaseAsset[] assets;
        public string         name;
    }

    private class ReleaseAsset
    {
        public string browser_download_url;
        public string name;
    }
}