using System.Text;
using CsToml;
using CsToml.Formatter.Resolver;
using TwentyTwentyTwenty.Data;

namespace TestProject;

public class TomlTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ColorSerializeTest()
    {
        TomlValueFormatterResolver.Register(new AppSettingsRecord.ColorTomlFormatter());
        var set = new AppSettingsRecord();
        var result = CsTomlSerializer.Serialize(set);
        Console.WriteLine(Encoding.UTF8.GetString(result.ByteSpan));
        Assert.Pass();
    }

    [Test]
    public void ColorDeserializeTest()
    {
        TomlValueFormatterResolver.Register(new AppSettingsRecord.ColorTomlFormatter());
        var toml = """
                   IntervalTime = 00:20:00
                   EscapeNextTime = 00:02:00
                   RestTime = 00:00:20
                   FadeInTime = 00:00:01
                   FadeOutTime = 00:00:00.8
                   RestFinishedColorChangeTime = 00:00:00.8
                   Invisibility = 96
                   CountdownColor = "Aqua"
                   FailedColor = "OrangeRed"
                   SuccessColor = "Gold"
                   """u8;
        var set = CsTomlSerializer.Deserialize<AppSettingsRecord>(toml);
        Console.WriteLine(set.ToString());
        Assert.Pass();
    }
}