using System.Text;
using CsToml;
using CsToml.Error;
using CsToml.Formatter.Resolver;
using TwentyTwentyTwenty.Data;

namespace TestProject;

public class SettingsTest
{
    [Test]
    public void CheckValidate()
    {
        TomlValueFormatterResolver.Register(new AppSettingsRecord.ColorTomlFormatter());
        const string toml = """
                            IntervalTime = 00:10:00                  # 过短。
                            EscapeNextTime = 00:12:20                # 过短；过长。
                            RestTime = 00:13:10.5                    # 过长。
                            FadeInTime = 00:00:08                    # 过长。
                            FadeOutTime = 00:00:09.8                 # 过长；毫秒。
                            RestFinishedColorChangeTime = 00:00:00   # 非正。
                            Invisibility = 255
                            CountdownColor = "Aqua"
                            FailedColor = "OrangeRed"
                            SuccessColor = "Gold"
                            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(toml));
        try
        {
            var appSettingsRecord = CsTomlSerializer.Deserialize<AppSettingsRecord>(stream);
            AppSettingsChecker.Validate(appSettingsRecord, out var errors);
            Console.WriteLine(string.Join('\n', errors));
            Assert.That(errors, Has.Count.EqualTo(8));
            Assert.Pass();
        }
        catch (CsTomlSerializeException e)
        {
            Console.WriteLine(string.Join('\n', e.ParseExceptions!.Select(it => it.ToString())));
            Assert.Fail();
        }
    }

    private static void TryLoadToml(string toml, int i = -1)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(toml));
        try
        {
            var res = CsTomlSerializer.Deserialize<AppSettingsRecord>(stream);
        }
        catch (CsTomlSerializeException exception)
        {
            var errors = exception.ParseExceptions!
                .Select(e => $"行号{e.LineNumber}: {e.InnerException?.Message ?? "未知错误。"}")
                .ToList();
            if (exception.InnerException is not null)
            {
                errors.Add("某处：" + exception.InnerException.Message);
            }


            Console.WriteLine(string.Join('\n', errors));
        }
        catch (ArgumentException exception)
        {
            Console.WriteLine("某处：" + exception.Message);
        }

        Console.WriteLine($"{i}: Syntax check done.");
    }

    [Test]
    public static void Syntax()
    {
        TomlValueFormatterResolver.Register(new AppSettingsRecord.ColorTomlFormatter());

        foreach (var (it, i) in SyntaxTestList.Select((it, i) => (it, i)))
        {
            TryLoadToml(it, i);
        }

        Assert.Pass();
    }

    private static readonly string[] SyntaxTestList =
    [
        // 0
        """
        IntervalTime = 00:10:00
        EscapeNextTime = 00:12:20
        RestTime = -00:13:10.5 ### 
        FadeInTime = 00:00:08
        FadeOutTime = 00:00:09.8
        RestFinishedColorChangeTime = 00:00:00
        Invisibility = 255
        CountdownColor = "Aqua"
        FailedColor = "OrangeRed"
        SuccessColor = "Gold"
        """,
        // 1
        """
        IntervalTime = 00:10:00
        EscapeNextTime = 00:12:20
        RestTime = 1234 ###
        FadeInTime = 00:00:08
        FadeOutTime = 00:00:09.8
        RestFinishedColorChangeTime = 00:00:00
        Invisibility = 255
        CountdownColor = "Aqua"
        FailedColor = "OrangeRed"
        SuccessColor = "Gold"
        """,
        // 2
        """
        IntervalTime = 00:10:00
        EscapeNextTime = 00:12:20
        RestTime = ###
        FadeInTime = 00:00:08
        FadeOutTime = 00:00:09.8
        RestFinishedColorChangeTime = 00:00:00
        Invisibility = 255
        CountdownColor = "Aqua"
        FailedColor = "OrangeRed"
        SuccessColor = "Gold"
        """,
        // 3
        """
        IntervalTime = 00:10:61 ###
        EscapeNextTime = 00:12:20
        RestTime = ###
        FadeInTime = 00:00:08
        FadeOutTime = 00:00:09.8
        RestFinishedColorChangeTime = 00:00:00
        Invisibility = 255
        CountdownColor = "Aqua"
        FailedColor = "OrangeRed"
        SuccessColor = "Gold"
        """,
        // 4：经测试这个不会报语法错误，而会把 RestTime 置 00:00:00，因此我们需要在 Validate 中检验出来
        """
        IntervalTime = 00:10:00
        EscapeNextTime = 00:12:20
        # RestTime = 
        FadeInTime = 00:05:08
        FadeOutTime = 00:05:09.8
        RestFinishedColorChangeTime = 00:00:29
        Invisibility = 255
        CountdownColor = "Aqua"
        FailedColor = "OrangeRed"
        SuccessColor = "Gold"
        """,
        // 5
        """
        IntervalTime = 00:20:00
        EscapeNextTime = 00:02:00
        RestTime = 00:00:20
        FadeInTime = 00:00:01
        FadeOutTime = 00:00:00.8
        RestFinishedColorChangeTime = 00:00:00.8
        Invisibility = 96
        CountdownColor = "Invalid"
        FailedColor = "#Invalid"
        SuccessColor = 0x80808080
        """,
        // 6
        """
        IntervalTime = 00:20:00
        EscapeNextTime = 00:02:00
        RestTime = 00:00:20
        FadeInTime = 00:00:01
        FadeOutTime = 00:00:00.8
        RestFinishedColorChangeTime = 00:00:00.8
        Invisibility = 96
        CountdownColor = "Red"
        FailedColor = "#Invalid"
        SuccessColor = 0x80808080
        """,
        // 7
        """
        IntervalTime = 00:20:00
        EscapeNextTime = 00:02:00
        RestTime = 00:00:20
        FadeInTime = 00:00:01
        FadeOutTime = 00:00:00.8
        RestFinishedColorChangeTime = 00:00:00.8
        Invisibility = 96
        CountdownColor = "Red"
        FailedColor = "#FFFFFFFF"
        SuccessColor = 0x8080808080
        """,
        // 8
        """
        IntervalTime = 00:20:00
        EscapeNextTime = 00:02:00
        RestTime = 00:00:20
        FadeInTime = 00:00:01
        FadeOutTime = 00:00:00.8
        RestFinishedColorChangeTime = 00:00:00.8
        Invisibility = 96
        CountdownColor = "Red"
        FailedColor = "#FFFFFFFF"
        SuccessColor = ###
        """
    ];
}