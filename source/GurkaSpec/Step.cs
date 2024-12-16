namespace SpecGurka.GurkaSpec;

public class Step
{
    public required string Kind { get; set; }
    public required string Text { get; set; }
    public string? Table { get; set; }

    public string TestDurationSeconds { get; set; }
    public string? TestErrorMessage { get; set; }
    public string? TestMethod { get; set; }
        
    public Status Status { get; set; }
}