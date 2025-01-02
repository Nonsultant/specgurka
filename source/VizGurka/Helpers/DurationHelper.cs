namespace VizGurka.Helpers
{
    public static class DurationHelper
    {
        public static string FormatDuration(string stringDuration)
        {
            TimeSpan duration = TimeSpan.Parse(stringDuration);

            if (duration.TotalMinutes >= 1)
            {
                return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
            }

            return $"{duration.Seconds}s";
        }
    }
}