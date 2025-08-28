using System.IO;
using CsToml;
using CsToml.Formatter.Resolver;

namespace TwentyTwentyTwenty.Data;

public static class AppSettingsLoader
{
    private static readonly string DirPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TwentyTwentyTwenty");

    internal static readonly string FilePath =
        Path.Combine(DirPath, "settings.toml");

    static AppSettingsLoader()
    {
        TomlValueFormatterResolver.Register(new AppSettingsRecord.ColorTomlFormatter());
    }

    private static void WriteToFile(AppSettingsRecord settings, FileInfo file)
    {
        var appendix = """
                       # 颜色表示方式：
                       # 1. Color = "Red"
                       # 2. Color = "#FFFF0000"
                       # 3. Color = 0xFFFF0000
                       # 方法1.的可用颜色列表：参见https://learn.microsoft.com/en-us/dotnet/media/art-color-table.png
                       #
                       # Invisibility 应该是一个介于0到255（包含）的整数。您也可以写作0x00到0xFF。
                       # 
                       """u8;

        var result = CsTomlSerializer.Serialize(settings);
        using var stream = file.Create();
        stream.Write(result.ByteSpan);
        stream.Write(appendix);
    }

    public static AppSettingsRecord Load()
    {
        if (!File.Exists(FilePath))
        {
            var settings = new AppSettingsRecord();
            Directory.CreateDirectory(DirPath);
            WriteToFile(settings, new FileInfo(FilePath));
            return settings;
        }

        using var stream = File.OpenRead(FilePath);
        var appSettingsRecord = CsTomlSerializer.Deserialize<AppSettingsRecord>(stream);
        return appSettingsRecord;
    }
}