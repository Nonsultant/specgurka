namespace ReqnrollDemoTwo.Spec.StepDefinitions
{
    [Binding]
    public class ManageSectionsStepDefinitions
    {
        private Warehouse _warehouse = null!;
        private Exception? _exception;

        [BeforeScenario]
        public void BeforeScenario()
        {
            _warehouse = new Warehouse("My Warehouse", "John Doe");
        }

        [Given(@"a section ""(.*)"" exists in the warehouse")]
        public void GivenASectionExistsInTheWarehouse(string sectionName)
        {
            var section = new Sections(sectionName);
            _warehouse.AddSection(section);
        }

        [When(@"I remove the section ""(.*)""")]
        public void WhenIRemoveTheSection(string sectionName)
        {
            _warehouse.RemoveSection(sectionName);
        }

        [Then(@"""(.*)"" should no longer exist in the list of sections")]
        public void ThenShouldNoLongerExistInTheListOfSections(string sectionName)
        {
            var sectionExists = _warehouse.Sections.Any(s => s.Name == sectionName);
            sectionExists.Should().BeFalse();
        }

        [Given(@"the section ""(.*)"" exists in the warehouse")]
        public void GivenTheSectionExistsInTheWarehouse(string sectionName)
        {
            var section = new Sections(sectionName);
            _warehouse.AddSection(section);
        }

        [When(@"I rename the section ""(.*)"" to ""(.*)""")]
        public void WhenIRenameTheSectionTo(string oldName, string newName)
        {
            _warehouse.RenameSection(oldName, newName);
        }

        [Then(@"the section name should be updated to ""(.*)""")]
        public void ThenTheSectionNameShouldBeUpdatedTo(string newName)
        {
            var sectionExists = _warehouse.Sections.Any(s => s.Name == newName);
            sectionExists.Should().BeTrue();
        }

        [Then(@"""(.*)"" should no longer exist in the list of sections for the warehouse")]
        public void ThenShouldNoLongerExistInTheListOfSectionsForTheWarehouse(string oldName)
        {
            var sectionExists = _warehouse.Sections.Any(s => s.Name == oldName);
            sectionExists.Should().BeFalse();
        }

        [Given(@"I am logged in as an admin user with permissions to manage sections")]
        public void GivenIAmLoggedInAsAnAdminUserWithPermissionsToManageSections()
        {
            // Implement login logic here
        }

        [Given(@"I go to the ""(.*)"" page")]
        public void GivenIGoToThePage(string pageName)
        {
            // Implement navigation logic here
        }
    }
}