using System.IO;
using System.Linq;
using MelonLoader.Utils;
using UnityEngine;

namespace NeoModLoader.AutoUpdate;

internal class Paths
{
    
    public static string GamePath => MelonEnvironment.GameRootDirectory;

    public static string NMLPath { get; internal set; }

    public static string NMLPdbPath => Combine(GamePath, "Mods", "NeoModLoader_mobile.pdb");
    private static string Combine(params string[] paths) => new FileInfo(paths.Aggregate("", Path.Combine)).FullName;
}