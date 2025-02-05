namespace ReqnrollDemoTwo.Spec.StepDefinitions
{
    [Binding]
    public class ManageWarehouseStepDefinitions
    {
        private Warehouse? _warehouse;
        private Exception? _exception;

        [AfterScenario]
        public void AfterScenario()
        {
            _warehouse = null;
            _exception = null;
        }

        [Given(@"the warehouse has a Category named ""(.*)""")]
        public void GivenTheWarehouseHasACategoryNamed(string categoryName)
        {
            _warehouse = new Warehouse("My Warehouse", categoryName);
        }

        [When(@"I change the Category to ""(.*)""")]
        public void WhenIChangeTheCategoryTo(string newCategoryName)
        {
            try
            {
                _warehouse!.ChangeCategory(newCategoryName);
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        [Then(@"the Category of the warehouse should be ""(.*)""")]
        public void ThenTheCategoryOfTheWarehouseShouldBe(string categoryName)
        {
            _warehouse!.Category.Should().BeEquivalentTo(categoryName);
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
}