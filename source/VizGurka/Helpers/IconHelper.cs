using SpecGurka.GurkaSpec;

namespace VizGurka.Helpers;

public class IconHelper
{
    public static string GetStatusIcon(Status status)
    {
        return status switch
        {
            Status.Passed => "passed",
            Status.Failed => "failed",
            Status.NotImplemented => "pending",
            _ => "cross"
        };
    }
}