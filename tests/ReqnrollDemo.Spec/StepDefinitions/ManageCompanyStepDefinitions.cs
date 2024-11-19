namespace ReqnrollDemo.Spec.StepDefinitions;

[Binding]
public class ManageCompanyStepDefinitions
{
    private Company? _company;
    private Exception? _exception;

    [AfterScenario]
    public void AfterScenario()
    {
        _company = null;
        _exception = null;
    }

    [Given(@"the company has a CEO named ""(.*)""")]
    public void GivenTheCompanyHasAceoNamed(string ceoName)
    {
        _company = new Company("My Company", ceoName);
    }

    [When(@"I change the CEO to ""(.*)""")]
    public void WhenIChangeTheCeoTo(string newCeoName)
    {
        try
        {
            _company!.ChangeCeo(newCeoName);
        }
        catch(Exception ex)
        {
            _exception = ex;
        }
    }

    [Then(@"the CEO of the company should be ""(.*)""")]
    public void ThenTheCeoOfTheCompanyShouldBe(string ceoName)
    {
        _company!.Ceo.Should().BeEquivalentTo(ceoName);
    }

    [Then(@"It should fail")]
    public void ThenItShouldFail()
    {
        _exception.Should().BeOfType<InvalidOperationException>();
    }

    [Then(@"It should fail and not let me change")]
    public void ThenItShouldFailAndNotLetMeChange()
    {
        _exception.Should().BeOfType<InvalidOperationException>();
    }
}