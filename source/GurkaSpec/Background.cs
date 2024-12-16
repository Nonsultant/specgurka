namespace SpecGurka.GurkaSpec;

public class Background
{
    public string? Name { get; set; }
    public string? Description { get; set; }

    public Status Status
    {
        get
        {
            if (Steps.Any(step => step.Status == Status.NotImplemented))
            {
                return Status.NotImplemented;
            }

            if (Steps.All(step => step.Status == Status.Passed))
            {
                return Status.Passed;
            }

            return Status.Failed;
        }
        set { }
    }

    public List<Step> Steps { get; set; } = [];

    public List<Step> GetSteps(string kind, string text)
    {
        var steps = Steps.Where(s => text.Contains(s.Text) && s.Kind == kind).ToList();
        return steps;
    }
}