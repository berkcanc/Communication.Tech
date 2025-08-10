namespace communication_tech.Helper;

public static class TimeHelper
{
    private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

    public static (DateTime startUtc, DateTime endUtc) GetUtcStartEndFromTurkeyTime(DateTime? startTime, DateTime? endTime)
    {
        var end = endTime ?? DateTime.UtcNow;
        var start = startTime ?? end.AddHours(-1); // default last hour
        
        // var endTr = endTimeTr ?? TimeZoneInfo.ConvertTime(DateTime.Now, TurkeyTimeZone);
        // var startTr = startTimeTr ?? endTr.AddHours(-1);
        //
        // var startUtc = TimeZoneInfo.ConvertTimeToUtc(startTr, TurkeyTimeZone);
        // var endUtc = TimeZoneInfo.ConvertTimeToUtc(endTr, TurkeyTimeZone);

        return (start, end);
    }
}