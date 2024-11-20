namespace GurkaSpec.UnitTests;

public class ScenarioTests
{

    private readonly Scenario _scenario = new Scenario()
    {
        Name = "Some scenario",
        Steps =
        [
            new Step { Kind = "Given", Text = "some precondition" },
            new Step { Kind = "When", Text = "some action is performed" },
            new Step { Kind = "Then", Text = "some result is expected" }
        ]
    };

    [Fact]
    public void ParseTestOutput_GivenAPassingScenario_ShouldParseCorrectly()
    {
        // Arrange
        string output = "Given some precondition\n" +
                        "-> done: Tests.GivenSomePrecondition (1.123s)\n" +
                        "When some action is performed\n" +
                        "-> done: Tests.WhenSomeActionIsPerformed (0.456s)\n" +
                        "Then some result is expected\n" +
                        "-> done: Tests.ThenSomeResultIsExpected (0.789s)";

        // Act
        _scenario.ParseTestOutput(output);

        // Assert
        _scenario.Steps.ForEach(step => step.TestPassed.Should().BeTrue());
    }

    [Fact]
    public void ParseTestOutput_GivenAFailingScenario_ShouldParseCorrectly()
    {
        // Arrange
        string output = "Given some precondition\n" +
                        "-> done: Tests.GivenSomePrecondition (1.123s)\n" +
                        "When some action is performed\n" +
                        "-> done: Tests.WhenSomeActionIsPerformed (0.456s)\n" +
                        "Then some result is expected\n" +
                        "-> error: Tests.ThenSomeResultIsExpected Failure: Strings differ\n" +
                        "Expected: \"Expected\"\n" +
                        "Actual: \"Not-Expected\"\n" +
                        "\u2191 (pos 11) (0.0s)<";

        // Act
        _scenario.ParseTestOutput(output);

        // Assert
        _scenario.TestPassed.Should().BeFalse();

        _scenario.Steps[0].TestPassed.Should().BeTrue();
        _scenario.Steps[1].TestPassed.Should().BeTrue();
        _scenario.Steps[2].TestPassed.Should().BeFalse();
    }

}