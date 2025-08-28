using System.Buffers;
using System.Reflection;
using System.Windows.Media;
using CsToml;
using CsToml.Error;
using CsToml.Formatter;
using CsToml.Values;

namespace TwentyTwentyTwenty.Data;

[TomlSerializedObject]
public partial record AppSettingsRecord
{
    /// <summary>
    /// 两次休息的间隔时间
    /// </summary>
    [TomlValueOnSerialized] public TimeOnly IntervalTime { get; init; } = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(20.0));

    /// <summary>
    /// 强制退出休息后，下次休息的时间
    /// </summary>
    [TomlValueOnSerialized] public TimeOnly EscapeNextTime { get; init; } = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(2.0));

    /// <summary>
    /// 休息的时间
    /// </summary>
    [TomlValueOnSerialized] public TimeOnly RestTime { get; init; } = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(20.0));

    /// <summary>
    /// 动画 FadeIn 的时间
    /// </summary>
    [TomlValueOnSerialized] public TimeOnly FadeInTime { get; init; } = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(1.0));

    /// <summary>
    /// 动画 FadeOut 的时间
    /// </summary>
    [TomlValueOnSerialized] public TimeOnly FadeOutTime { get; init; } = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(0.8));

    /// <summary>
    /// 即将退出休息时的颜色改变动画的时间
    /// </summary>
    [TomlValueOnSerialized] public TimeOnly RestFinishedColorChangeTime { get; init; } = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(0.8));

    /// <summary>
    /// 倒计时的颜色
    /// </summary>
    [TomlValueOnSerialized] public Color CountdownColor { get; init; } = Colors.Cyan;

    /// <summary>
    /// 强制退出休息时倒计时的颜色
    /// </summary>
    [TomlValueOnSerialized] public Color FailedColor { get; init; } = Colors.OrangeRed;

    /// <summary>
    /// 倒计时正常结束的颜色
    /// </summary>
    [TomlValueOnSerialized] public Color SuccessColor { get; init; } = Colors.Gold;

    /// <summary>
    /// 蒙板的遮盖程度
    /// </summary>
    [TomlValueOnSerialized] public byte Invisibility { get; init; } = 0x60;

    public class ColorTomlFormatter : ITomlValueFormatter<Color>
    {
        private static readonly Type ColorType = typeof(Colors);

        private static readonly PropertyInfo[] ColorProps =
            ColorType.GetProperties(BindingFlags.Public | BindingFlags.Static);

        private static Color GetColorByName(string name) =>
            (Color)(ColorProps
                .FirstOrDefault(it => it.Name == name)
                ?.GetValue(null) ?? throw new ArgumentException($"无效颜色：{name}。"));

        private static string GetNameByColor(Color color) =>
            ColorProps
                .FirstOrDefault(it => (Color)it.GetValue(null)! == color)
                ?.Name ?? color.ToString();

        private static Color GetColorByArgbInt(uint argb)
        {
            var a = (byte)((argb & 0xFF000000) >> 24);
            var r = (byte)((argb & 0x00FF0000) >> 16);
            var g = (byte)((argb & 0x0000FF00) >> 8);
            var b = (byte)((argb & 0x000000FF) >> 0);
            return Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// 略
        /// </summary>
        /// <param name="rootNode"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">无效的颜色</exception>
        public Color Deserialize(ref TomlDocumentNode rootNode, CsTomlSerializerOptions options)
        {
            if (!rootNode.HasValue) throw new ArgumentException("颜色为空。");
            switch (rootNode.ValueType)
            {
                case TomlValueType.String:
                    var colorStr = rootNode.GetString();
                    if (!colorStr.StartsWith('#'))
                        return GetColorByName(colorStr);
                    try
                    {
                        var colorInt1 = Convert.ToUInt32(colorStr[1..], 16);
                        return GetColorByArgbInt(colorInt1);
                    }
                    catch (Exception)
                    {
                        throw new ArgumentException($"无效颜色：{colorStr}。");
                    }
                case TomlValueType.Integer:
                    var num = (ulong)rootNode.GetInt64();
                    if ((num & 0xFFFFFFFF) != num)
                    {
                        throw new ArgumentException($"无效颜色：0x{num:x}。");
                    }
                    var colorInt2 = (uint)num;
                    return GetColorByArgbInt(colorInt2);
                case TomlValueType.Key:
                case TomlValueType.Empty:
                case TomlValueType.Float:
                case TomlValueType.Boolean:
                case TomlValueType.OffsetDateTime:
                case TomlValueType.LocalDateTime:
                case TomlValueType.LocalDate:
                case TomlValueType.LocalTime:
                case TomlValueType.Array:
                case TomlValueType.Table:
                case TomlValueType.InlineTable:
                default:
                    return default;
            }
        }

        public void Serialize<TBufferWriter>(ref Utf8TomlDocumentWriter<TBufferWriter> writer, Color target,
            CsTomlSerializerOptions options) where TBufferWriter : IBufferWriter<byte>
        {
            writer.WriteString(GetNameByColor(target));
        }
    }
}