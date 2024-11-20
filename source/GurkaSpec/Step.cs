namespace SpecGurka.GurkaSpec;

public class Step
{
    public string Text { get; set; }
    public string StepMethod { get; set; }
    public string Kind { get; set; }

    public string TestDurationSeconds { get; set; }
    public string TestErrorMessage { get; set; }
    public string TestMethod { get; set; }
        
    public bool TestPassed { get; set; }
}