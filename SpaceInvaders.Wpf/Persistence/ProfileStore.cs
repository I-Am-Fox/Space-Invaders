using System;
using System.IO;
using System.Text.Json;
using SpaceInvaders.Core.Upgrades;

namespace SpaceInvaders.Wpf.Persistence;

public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    private static readonly string ProfilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SpaceInvaders",
        "profile.json");

    public MetaProgression LoadOrCreate()
    {
        try
        {
            if (!File.Exists(ProfilePath))
                return MetaProgression.Default;

            var json = File.ReadAllText(ProfilePath);
            return JsonSerializer.Deserialize<MetaProgression>(json, Options) ?? MetaProgression.Default;
        }
        catch
        {
            return MetaProgression.Default;
        }
    }

    public void Save(MetaProgression meta)
    {
        try
        {
            var dir = Path.GetDirectoryName(ProfilePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(meta, Options);
            File.WriteAllText(ProfilePath, json);
        }
        catch
        {
            // ignore
        }
    }
}
