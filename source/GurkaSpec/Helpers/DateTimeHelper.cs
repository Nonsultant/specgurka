using System;
using System.Globalization;

namespace GurkaSpec.Helpers
{
    public static class DateTimeHelper
    {
        public static string FormatDateTimeForCulture(DateTime utcDateTime, string cultureName)
        {
            var culture = new CultureInfo(cultureName);
            var timeZone = GetTimeZoneForCulture(culture);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
            return localTime.ToString("g", culture);
        }

        private static TimeZoneInfo GetTimeZoneForCulture(CultureInfo culture)
        {
            return culture.Name switch
            {
                "sv-SE" => TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time"),
                "en-GB" => TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"),
                _ => TimeZoneInfo.Utc
            };
        }
    }
}