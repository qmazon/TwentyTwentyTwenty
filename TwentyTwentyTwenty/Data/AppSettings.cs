using System.Diagnostics.CodeAnalysis;
using System.IO;
using Tomlyn;
using Tomlyn.Model;

namespace TwentyTwentyTwenty.Data;

public class AppSettings
{
    public double IntervalMinutes { get; set; } = 20;
    public int RestSeconds { get; set; } = 20;
    public double FadeInSeconds { get; set; } = 1.5;
    public double FadeOutSeconds { get; set; } = 1.5;
    public double EscapeNextMinutes { get; set; } = 2.0;

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
            // 首次运行：生成默认文件。无须进行Validate检查。
            var def = new AppSettings();
            Save(def);
            return def;
        }

        TomlTable tbl;
        try
        {
            var toml = File.ReadAllText(FilePath);
            tbl = Toml.ToModel(toml);
        }
        catch (TomlException te)
        {
            throw new ArgumentException("配置文件格式错误，（行号，列数）是" + te.Message, te);
        }

        var intervalMinutes = ReadDouble(tbl, "interval_minutes");
        var restSeconds = ReadInt32(tbl, "rest_seconds");
        var fadeIn = ReadDouble(tbl, "fade_in_seconds");
        var fadeOut = ReadDouble(tbl, "fade_out_seconds");
        var escape = ReadDouble(tbl, "escape_next_minutes");

        var settings = new AppSettings
        {
            IntervalMinutes = intervalMinutes,
            RestSeconds = restSeconds,
            FadeInSeconds = fadeIn,
            FadeOutSeconds = fadeOut,
            EscapeNextMinutes = escape
        };

        Validate(settings);
        return settings;
    }

    private static void Save(AppSettings cfg)
    {
        var toml = Toml.FromModel(cfg);
        Directory.CreateDirectory(DirPath);
        File.WriteAllText(FilePath, toml);
    }

    private static double ReadDouble(TomlTable tbl, string key)
    {
        try
        {
            return Convert.ToDouble(tbl[key]);
        }
        catch (KeyNotFoundException)
        {
            throw new ArgumentException($"缺少必要字段：{key}");
        }
        catch (Exception ex)
            when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new ArgumentException($"字段 {key} 必须是合法的数字。{ex.Message}");
        }
    }

    private static int ReadInt32(TomlTable tbl, string key)
    {
        try
        {
            return Convert.ToInt32(tbl[key]);
        }
        catch (KeyNotFoundException)
        {
            throw new ArgumentException($"缺少必要字段：{key}");
        }
        catch (Exception ex)
            when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new ArgumentException($"字段 {key} 必须是合法的数字。{ex.Message}");
        }
    }

    [SuppressMessage("Usage", "CA2208:正确实例化参数异常")]
    private static void Validate(AppSettings s)
    {
        const double minutesPerDay = 24 * 60;

        if (s.IntervalMinutes is <= 0 or >= minutesPerDay)
            throw new ArgumentOutOfRangeException(
                nameof(s.IntervalMinutes),
                $"interval_minutes 必须是大于 0 且小于 {minutesPerDay} 的正数。");

        if (s.RestSeconds is <= 0 or > 99)
            throw new ArgumentOutOfRangeException(
                nameof(s.RestSeconds),
                "rest_seconds 必须是 1~99 之间的正整数。");

        if (s.FadeInSeconds is < 0 or > 5.0)
            throw new ArgumentOutOfRangeException(
                nameof(s.FadeInSeconds),
                "fade_in_seconds 必须是 0.0~5.0 之间的非负数。");

        if (s.FadeOutSeconds is < 0 or > 5.0)
            throw new ArgumentOutOfRangeException(
                nameof(s.FadeOutSeconds),
                "fade_out_seconds 必须是 0.0~5.0 之间的非负数。");
        
        if (s.EscapeNextMinutes > s.IntervalMinutes || s.EscapeNextMinutes <= s.FadeOutSeconds / 60.0)
            throw new ArgumentOutOfRangeException(
                nameof(s.EscapeNextMinutes),
                "escape_next_minutes 必须不大于 interval_minutes、大于 fade_out_seconds。");

        var totalSeconds = s.FadeInSeconds + s.FadeOutSeconds + s.RestSeconds;
        if (totalSeconds >= s.IntervalMinutes * 60)
            throw new ArgumentOutOfRangeException(
                null,
                "fade_in_seconds + fade_out_seconds + rest_seconds 必须小于 interval_minutes。");
    }
}