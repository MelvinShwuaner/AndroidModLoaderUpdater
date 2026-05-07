using System;
using System.Threading.Tasks;
using UnityEngine;

namespace NeoModLoader.AutoUpdate;
using static UpdateHelper;
public abstract class AUpdater
{
    /// <summary>
    /// </summary>
    /// <returns>Whether update to date now</returns>
    public abstract bool CheckUpdate();

    public abstract bool               IsAvailable();
    public abstract Task<UpdateResult> DownloadAndReplace();

    /// <summary>
    /// </summary>
    /// <returns>Whether update to date now</returns>
    public async Task<bool> Update()
    {
        if (!IsAvailable())
        {
            LogMsg($"{GetType().Name} is not available");
            return false;
        }

        try
        {
            if (CheckUpdate()) return true;
        }
        catch (Exception e)
        {
            LogMsg($"{GetType().Name} failed to check update. The reason is below:");
            LogMsg($"{e.Message}");
            LogMsg(e.StackTrace);
            return false;
        }

        try
        {
            UpdateResult res = await DownloadAndReplace();
            switch (res)
            {
                case UpdateResult.IsTaken:
                    return true;
                case UpdateResult.Success:
                    return true;
                case UpdateResult.NoNeedUpdate:
                    return true;
                case UpdateResult.Fail:
                    return false;
            }
        }
        catch (Exception e)
        {
            LogMsg($"{GetType().Name} failed to download and replace. The reason is below:");
            LogMsg($"{e.Message}");
            LogMsg(e.StackTrace);
        }

        return false;
    }
}