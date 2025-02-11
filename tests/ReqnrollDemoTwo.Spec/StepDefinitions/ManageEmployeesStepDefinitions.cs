namespace ReqnrollDemoTwo.Spec.StepDefinitions
{
    [Binding]
    public class ManageEmployeesStepDefinitions
    {
        private Warehouse _warehouse = null!;
        private Sections _section = null!;
        private Exception? _exception;

        [BeforeScenario]
        public void BeforeScenario()
        {
            _warehouse = new Warehouse("My Warehouse", "General");
        }

        [Given(@"I am logged in as an HR manager")]
        public void GivenIAmLoggedInAsAnHRManager()
        {
            // Implement login logic here
        }

        [Given(@"I am on the ""(.*)"" page")]
        public void GivenIAmOnThePage(string pageName)
        {
            // Implement navigation logic here
        }

        [Given(@"the section ""(.*)"" exists with the following employees:")]
        public void GivenTheSectionExistsWithTheFollowingEmployees(string sectionName, Table table)
        {
            _section = new Sections(sectionName);
            foreach (var row in table.Rows)
            {
                var employee = new Employee(row["Name"], Enum.Parse<Role>(row["Role"]));
                _section.AddEmployee(employee);
            }
            _warehouse.AddSection(_section);
        }

        [When(@"I add an employee ""(.*)"" with the role ""(.*)"" to the ""(.*)"" section")]
        public void WhenIAddAnEmployeeWithTheRoleToTheSection(string employeeName, string employeeRole, string sectionName)
        {
            var employee = new Employee(employeeName, Enum.Parse<Role>(employeeRole));
            _section?.AddEmployee(employee);
        }

        [Then(@"""(.*)"" should be added to the list of employees in the ""(.*)"" section")]
        public void ThenShouldBeAddedToTheListOfEmployeesInTheSection(string employeeName, string sectionName)
        {
            var employee = _section?.Employees.FirstOrDefault(e => e.Name == employeeName);
            employee.Should().NotBeNull();
        }

        [Given(@"an employee ""(.*)"" exists in the ""(.*)"" section")]
        public void GivenAnEmployeeExistsInTheSection(string employeeName, string sectionName)
        {
            _section = _warehouse.Sections.FirstOrDefault(s => s.Name == sectionName);
            var employee = new Employee(employeeName, Role.Manager); // Assuming a default role
            _section?.AddEmployee(employee);
        }

        [When(@"I remove ""(.*)"" from the ""(.*)"" section")]
        public void WhenIRemoveFromTheSection(string employeeName, string sectionName)
        {
            var employee = _section?.Employees.FirstOrDefault(e => e.Name == employeeName);
            if (employee != null)
            {
                _section.RemoveEmployee(employee.Id);
            }
        }

        [Then(@"""(.*)"" should no longer appear in the list of employees in the ""(.*)"" section")]
        public void ThenShouldNoLongerAppearInTheListOfEmployeesInTheSection(string employeeName, string sectionName)
        {
            var section = _warehouse.Sections.FirstOrDefault(s => s.Name == sectionName);
            var employeeExists = section?.Employees.Any(e => e.Name == employeeName) ?? false;
            employeeExists.Should().BeFalse();
        }
    }
}