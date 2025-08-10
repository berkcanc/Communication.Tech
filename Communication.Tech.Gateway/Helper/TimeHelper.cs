namespace communication_tech.Helper;

public static class TimeHelper
{
    private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

    public static (DateTime startUtc, DateTime endUtc) GetUtcStartEndFromTurkeyTime(DateTime? startTimeTr, DateTime? endTimeTr)
    {
        var endTr = endTimeTr ?? TimeZoneInfo.ConvertTime(DateTime.Now, TurkeyTimeZone);
        var startTr = startTimeTr ?? endTr.AddHours(-1);

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startTr, TurkeyTimeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endTr, TurkeyTimeZone);

        return (startUtc, endUtc);
    }
}