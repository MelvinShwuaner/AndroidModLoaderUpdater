using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
namespace NeoModLoader.AutoUpdate;
using static UpdateHelper;
[MelonLoader.RegisterTypeInIl2Cpp]
public class WorldBoxMod : MonoBehaviour
{
    public WorldBoxMod(IntPtr ptr) : base(ptr)
    {
    }

    public WorldBoxMod() : base(ClassInjector.DerivedConstructorPointer<WorldBoxMod>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }
    public static WorldBoxMod I              { get; private set; }
    public static Version     CurrentVersion { get; private set; }
    public void Awake()
    {
        I = this;
        var path1 = Path.Combine(MelonEnvironment.GameRootDirectory, "Mods", "NeoModLoader_mobile.dll");
        var path2 = Path.Combine(MelonEnvironment.GameRootDirectory, "Mods", "NeoModLoader_memload.dll");
        var both_existed = File.Exists(path1) && File.Exists(path2);
        var any_existed = File.Exists(path1) || File.Exists(path2);
        if (both_existed)
        {
            try
            {
                File.Delete(path2);
            }
            catch (Exception e)
            {
                File.Delete(path1);
            }
        }

        if (File.Exists(path2))
            Paths.NMLPath = path2;
        else
            Paths.NMLPath = path1;

        UpdateVersion();
        LogMsg($"Begin to check update for NML. Current version: {CurrentVersion}");
        var updaters = new List<AUpdater>
        {
            new GithubUpdater(),
           // new GiteeUpdater() //what the fuck is gitee
        };
        var async = false;
        foreach (AUpdater updater in updaters)
        {
            var awaiter_res = updater.Update();
            var no_async = awaiter_res.IsCompleted;
            async |= !no_async;
            /* Several situations:
             * 1. Mod loaded: NeoModLoader->AutoUpdate, no need to load NML manually
             * 2. Mod loaded: AutoUpdate->NeoModLoader, use cached download file: old file is deleted and restored in a single frame(no async). no need to load NML manually
             * 3. Mod loaded: AutoUpdate->NeoModLoader, download file: old file is deleted, start downloading new file, replace successfully(async). NML is loaded already.
             * 4. Mod loaded: AutoUpdate->NeoModLoader, download file: old file is deleted, start downloading new file, replace failed and restore old file(async). NML is not existed so that it should be loaded manually.
             * 5. Mod loaded: AutoUpdate->NeoModLoader, NML is not existed so that it should be loaded manually.
             */
            if (awaiter_res.GetAwaiter().GetResult())
            {
                UpdateVersion();
                LogMsg($"Updated to latest version: {CurrentVersion} from {updater.GetType().Name}");
                if ((!no_async && !IsNeoModLoaderLoaded()) || !any_existed)
                    UpdateHelper.LoadNMLManually();

                return;
            }
        }

        if (async && !IsNeoModLoaderLoaded()) UpdateHelper.LoadNMLManually();

        LogMsg($"No update available. Current version: {CurrentVersion}");
    }

    static bool IsNeoModLoaderLoaded()
    {
        if (ModLoader.modsLoaded.Contains("NeoModLoader"))
        {
            return true;
        }
        foreach (var mod in MelonMod.RegisteredMelons)
        {
            if (Path.GetFileName(mod.MelonAssembly.Location) == "NeoModLoader_mobile.dll")
            {
                return true;
            }   
        }

        return false;
    }

    internal void UpdateVersion()
    {
        if (File.Exists(Paths.NMLPath))
            CurrentVersion = AssemblyName.GetAssemblyName(Paths.NMLPath).Version;
        else
            CurrentVersion = new Version(0, 0, 0, 0);
    }
}