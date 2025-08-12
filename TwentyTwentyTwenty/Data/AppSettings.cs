using System.IO;
using Tomlyn;

namespace TwentyTwentyTwenty.Data;

public class AppSettings
{
    public double IntervalMinutes { get; set; } = 20;
    public int RestSeconds { get; set; } = 20;
    public double FadeInSeconds { get; set; } = 1.5;
    public double FadeOutSeconds { get; set; } = 1.5;

    private static readonly string DirPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TwentyTwentyTwenty");

    public static readonly string FilePath =
        Path.Combine(DirPath, "settings.toml");

    public static AppSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            // 首次运行：生成默认文件
            var def = new AppSettings();
            Save(def);
            return def;
        }

        var toml = File.ReadAllText(FilePath);
        var tbl  = Toml.ToModel(toml);
        return new AppSettings
        {
            IntervalMinutes = (double)tbl["interval_minutes"],
            RestSeconds = Convert.ToInt32((long)tbl["rest_seconds"]),
            FadeInSeconds = (double)tbl["fade_in_seconds"],
            FadeOutSeconds = (double)tbl["fade_out_seconds"]
        };
    }

    private static void Save(AppSettings cfg)
    {
        var toml = Toml.FromModel(cfg);
        Directory.CreateDirectory(DirPath);
        File.Create(FilePath).Close();
        File.WriteAllText(FilePath, toml);
    }
}