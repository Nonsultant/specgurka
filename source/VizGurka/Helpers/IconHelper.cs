using SpecGurka.GurkaSpec;

namespace VizGurka.Helpers;

public class IconHelper
{
    public static string GetStatusIcon(Status status)
    {
        return status switch
        {
            Status.Passed => "check",
            Status.Failed => "cross",
            Status.NotImplemented => "pending",
            _ => "cross"
        };
    }
}