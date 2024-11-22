namespace ReqnrollDemo.Spec.StepDefinitions;

[Binding]
public class ManageDepartmentsStepDefinitions
{
    private Company _company = null!;

    [BeforeScenario]
    public void BeforeScenario()
    {
        _company = new Company("My Company", "John Doe");
    }

    [Given(@"a department ""(.*)"" exists in the company")]
    public void GivenADepartmentExistsInTheCompany(string departmentName)
    {
        var department = new Department(departmentName);
        _company.AddDepartment(department);
    }

    [When(@"I remove the department ""(.*)""")]
    public void WhenIRemoveTheDepartment(string departmentName)
    {
        _company.RemoveDepartment(departmentName);
    }

    [Then(@"""(.*)"" should no longer exist in the list of departments")]
    public void ThenShouldNoLongerExistInTheListOfDepartments(string departmentName)
    {
        var companyExists = _company.Departments.Any(d => d.Name == departmentName);

        Assert.False(companyExists);
    }

    [Given(@"the department ""(.*)"" exists in the company")]
    public void GivenTheDepartmentExistsInTheCompany(string departmentName)
    {
        var department = new Department(departmentName);
        _company.AddDepartment(department);
    }

    [When(@"I rename the department ""(.*)"" to ""(.*)""")]
    public void WhenIRenameTheDepartmentTo(string oldName, string newName)
    {
        _company.RenameDepartment(oldName, newName);
    }

    [Then(@"the department name should be updated to ""(.*)""")]
    public void ThenTheDepartmentNameShouldBeUpdatedTo(string departmentName)
    {
        var department = _company.Departments.FirstOrDefault(d => d.Name == departmentName);

        department.Should().NotBeNull();
    }

    [Then(@"""(.*)"" should no longer exist in the list of departments for the company")]
    public void ThenShouldNoLongerExistInTheListOfDepartmentsForTheCompany(string oldName)
    {
        var departmentExists = _company.Departments.Any(d => d.Name == oldName);

        departmentExists.Should().BeFalse();
    }

    [Given(@"I am logged in as an admin user with permissions to manage departments")]
    public void GivenIAmLoggedInAsAnAdminUserWithPermissionsToManageDepartments()
    {
        Console.WriteLine("I am logged in as the CEO!");
    }

    [Given(@"I go to the ""(.*)"" page")]
    public void GivenIGoToThePage(string page)
    {
        Console.WriteLine($"Navigated to {page} page!");
    }


    [Given(@"I want to add a new department to the company")]
    public void GivenIWantToAddANewDepartmentToTheCompany()
    {

    }

    [When(@"I add a department with an empty name")]
    public void WhenIAddADepartmentWithAnEmptyName()
    {

    }

    [Then(@"It should fail with the message ""(.*)""")]
    public void ThenItShouldFailWithTheMessage(string p0)
    {
        var action = () => _company.AddDepartment(new Department(""));
        action.Should().Throw<InvalidOperationException>();
    }

    [When(@"I add the department ""(.*)""")]
    public void WhenIAddTheDepartment(string departmentName)
    {
        _company.AddDepartment(new Department(departmentName));
    }

    [Then(@"the department should be added to the list of departments in the company")]
    public void ThenTheDepartmentShouldBeAddedToTheListOfDepartmentsInTheCompany()
    {
        _company.Departments.Count.Should().BePositive();
    }

    [When(@"I add the department named ""(.*)""")]
    public void WhenIAddTheDepartmentNamed(string departmentName)
    {
        var action = () => _company.AddDepartment(new Department(""));
        action.Should().Throw<InvalidOperationException>();
    }
}