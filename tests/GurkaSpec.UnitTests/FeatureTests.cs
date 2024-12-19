namespace GurkaSpec.UnitTests;

public class FeatureTests
{
    private readonly Feature _feature;

    public FeatureTests()
    {
        _feature = new Feature()
        {
            Name = "Some feature",
            Scenarios =
            [
                new Scenario
                {
                    Name = "Some scenario",
                    Steps =
                    [
                        new Step { Kind = "Given", Text = "some precondition" },
                        new Step { Kind = "When", Text = "some action is performed" },
                        new Step { Kind = "Then", Text = "some result is expected" }
                    ]
                }
            ]
        };
    }

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
        _feature.ParseTestOutput(output, _feature.Scenarios[0]);

        // Assert
        _feature.Scenarios[0].Steps.ForEach(step => step.Status.Should().Be(Status.Passed));
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
                        "\u2191 (pos 11) (0.0s)";

        // Act
        _feature.ParseTestOutput(output, _feature.Scenarios[0]);

        // Assert
        _feature.Status.Should().Be(Status.Failed);
        _feature.Scenarios[0].Status.Should().Be(Status.Failed);
        _feature.Scenarios[0].Steps[0].Status.Should().Be(Status.Passed);
        _feature.Scenarios[0].Steps[1].Status.Should().Be(Status.Passed);
        _feature.Scenarios[0].Steps[2].Status.Should().Be(Status.Failed);
    }
}