namespace communication_tech.Helper;

public static class TimeHelper
{
    public static long ConvertUtcToUnixTimeWithTurkeyTime(DateTime dateTime)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException("DateTime must be UTC", nameof(dateTime));

        // UTC+3 offset
        var offset = TimeSpan.FromHours(3);
        var localDateTime = DateTime.SpecifyKind(dateTime.AddHours(3), DateTimeKind.Unspecified);
        var dto = new DateTimeOffset(localDateTime, offset);

        return dto.ToUnixTimeSeconds();
    }

}