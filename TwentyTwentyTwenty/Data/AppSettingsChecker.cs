namespace TwentyTwentyTwenty.Data;

public static class AppSettingsChecker
{
    /// <summary>
    /// 检查项目列表：<br/>
    /// + 所有 TimeOnly 都应该为正<br/>
    /// + EscapeNextTime 小于等于 IntervalTime<br/>
    /// + EscapeNextTime 和 IntervalTime 都需要大于 RestTime + FadeInTime + FadeOutTime + RestFinishedColorChangeTime<br/>
    /// + FadeInTime 和 FadeOutTime需要小于等于 5.0秒<br/>
    /// + IntervalTime 和 EscapeNextTime 需要是 1~99秒（包含）之间的数据。必须是整秒数，不能有毫秒
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="errors"></param>
    /// <returns>通过检查则返回 True，否则 False</returns>
    public static bool Validate(AppSettingsRecord settings, out List<string> errors)
    {
        var errorList = new List<string>();

        // 检查所有 TimeOnly 属性是否为正
        CheckPositiveTime(settings.IntervalTime, nameof(settings.IntervalTime), errorList);
        CheckPositiveTime(settings.EscapeNextTime, nameof(settings.EscapeNextTime), errorList);
        CheckPositiveTime(settings.RestTime, nameof(settings.RestTime), errorList);
        CheckPositiveTime(settings.FadeInTime, nameof(settings.FadeInTime), errorList);
        CheckPositiveTime(settings.FadeOutTime, nameof(settings.FadeOutTime), errorList);
        CheckPositiveTime(settings.RestFinishedColorChangeTime, nameof(settings.RestFinishedColorChangeTime),
            errorList);

        // 检查 EscapeNextTime <= IntervalTime
        if (settings.EscapeNextTime > settings.IntervalTime)
        {
            errorList.Add(
                $"{nameof(settings.EscapeNextTime)} 需要小于等于 {nameof(settings.IntervalTime)}。");
        }

        // 计算时间总和
        var totalRestTime = settings.RestTime.ToTimeSpan()
                            + settings.FadeInTime.ToTimeSpan()
                            + settings.FadeOutTime.ToTimeSpan()
                            + settings.RestFinishedColorChangeTime.ToTimeSpan();

        // 检查 EscapeNextTime 和 IntervalTime 是否大于总时间
        if (settings.EscapeNextTime.ToTimeSpan() <= totalRestTime)
        {
            errorList.Add(
                $"{nameof(settings.EscapeNextTime)} 需要大于 {nameof(settings.RestTime)}," +
                $" {nameof(settings.FadeInTime)}, {nameof(settings.FadeOutTime)}, " +
                $"{nameof(settings.RestFinishedColorChangeTime)} 这四者的和。");
        }

        if (settings.IntervalTime.ToTimeSpan() <= totalRestTime)
        {
            errorList.Add(
                $"{nameof(settings.IntervalTime)} 需要大于 {nameof(settings.RestTime)}," +
                $" {nameof(settings.FadeInTime)}, {nameof(settings.FadeOutTime)}, " +
                $"{nameof(settings.RestFinishedColorChangeTime)} 这四者的和。");
        }

        // 检查 FadeInTime 和 FadeOutTime 是否 <= 5 秒
        CheckMaxDuration(settings.FadeInTime, nameof(settings.FadeInTime), TimeSpan.FromSeconds(5), errorList);
        CheckMaxDuration(settings.FadeOutTime, nameof(settings.FadeOutTime), TimeSpan.FromSeconds(5), errorList);

        // 检查 Rest 是否为整秒数且在 1~99 秒之间
        CheckIntegerSeconds(settings.RestTime, nameof(settings.RestTime), 1, 99, errorList);

        errors = errorList;
        return errorList.Count == 0;
    }

    private static void CheckPositiveTime(TimeOnly time, string propertyName, ICollection<string> errorList)
    {
        if (time.ToTimeSpan() <= TimeSpan.Zero)
        {
            errorList.Add($"{propertyName} 需要为正。");
        }
    }

    private static void CheckMaxDuration(TimeOnly time, string propertyName, TimeSpan maxDuration,
        List<string> errorList)
    {
        if (time.ToTimeSpan() > maxDuration)
        {
            errorList.Add($"{propertyName} 需要小于等于 {maxDuration.TotalSeconds} 秒。");
        }
    }

    private static void CheckIntegerSeconds(TimeOnly time, string propertyName, int minSeconds, int maxSeconds,
        List<string> errorList)
    {
        var timeSpan = time.ToTimeSpan();

        // 检查是否为整秒数（没有毫秒）
        if (timeSpan.Milliseconds != 0)
        {
            errorList.Add($"{propertyName} 需要是整秒数（没有毫秒）。");
        }

        // 检查是否在指定范围内
        if (timeSpan.TotalSeconds < minSeconds || timeSpan.TotalSeconds > maxSeconds)
        {
            errorList.Add($"{propertyName} 需要介于 {minSeconds} 秒和 {maxSeconds} 秒之间。");
        }
    }
}