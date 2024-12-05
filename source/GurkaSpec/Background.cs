namespace SpecGurka.GurkaSpec;

public class Background
{
    public string? Name { get; set; }
    public string? Description { get; set; }

    public bool TestsPassed
    {
        get
        {
            bool testsPassed = true;
            foreach (var step in Steps)
            {
                if (!step.TestPassed)
                {
                    testsPassed = false;
                    break;
                }
            }

            return testsPassed;
        }
        set {}
    }

    public List<Step> Steps { get; set; } = [];

    public List<Step> GetSteps(string kind, string text)
    {
        var steps = Steps.Where(s => text.Contains(s.Text) && s.Kind == kind).ToList();
        return steps;
    }
}