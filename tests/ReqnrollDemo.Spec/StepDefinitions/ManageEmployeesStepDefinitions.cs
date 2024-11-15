namespace ReqnrollDemo.Spec.StepDefinitions;

[Binding]
public class ManageEmployeesStepDefinitions
{
    private Department? _department;

    [BeforeScenario]
    public void BeforeScenario()
    {
        _department = null;
    }

    [Given(@"the department ""(.*)"" exists")]
    public void GivenTheDepartmentExists(string departmentName)
    {
        _department = new Department(departmentName);
    }

    [When(@"I add an employee ""(.*)"" with the role ""(.*)"" to the ""(.*)"" department")]
    public void WhenIAddAnEmployeeWithTheRoleToTheDepartment(string employeeName, string employeeRole, string departmentName)
    {
        var employee = new Employee(employeeName, Enum.Parse<Role>(employeeRole));
        _department?.AddEmployee(employee);
    }

    [Then(@"""(.*)"" should be added to the list of employees in the ""(.*)"" department")]
    public void ThenShouldBeAddedToTheListOfEmployeesInTheDepartment(string employeeName, string departmentName)
    {
        var employee = _department?.Employees.FirstOrDefault(e => e.Name == employeeName);

        Assert.NotNull(employee);
    }

    [Given(@"an employee ""(.*)"" exists in the ""(.*)"" department")]
    public void GivenAnEmployeeExistsInTheDepartment(string employeeName, string departmentName)
    {
        _department = new Department(departmentName);
        var employee = new Employee(employeeName, Role.Engineer);
        _department.AddEmployee(employee);
    }

    [When(@"I remove ""(.*)"" from the ""(.*)"" department")]
    public void WhenIRemoveFromTheDepartment(string employeeName, string departmentName)
    {
        var employee = _department?.Employees.FirstOrDefault(e => e.Name == employeeName);
        _department?.RemoveEmployee(employee.Id);
    }

    [Then(@"""(.*)"" should no longer appear in the list of employees in the ""(.*)"" department")]
    public void ThenShouldNoLongerAppearInTheListOfEmployeesInTheDepartment(string employeeName, string departmentName)
    {
        var employee = _department?.Employees.FirstOrDefault(e => e.Name == employeeName);

        employee.Should().BeNull();
    }
}