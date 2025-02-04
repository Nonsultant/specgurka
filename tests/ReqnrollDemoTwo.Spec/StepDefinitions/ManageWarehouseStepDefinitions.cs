namespace ReqnRollDemoTwo.Spec.StepDefinitions
{
    [Binding]
    public class ManageWarehouseStepDefinitions
    {
        private Warehouse? _warehouse;

        [AfterScenario]
        public void AfterScenario()
        {
            _warehouse = null;
        }

        [Given(@"the warehouse has a Category named ""(.*)""")]
        public void GiventhewarehousehasaCategorynamed(string args1)
        {
            _warehouse.Pending();
        }

        [When(@"I change the Category to ""(.*)""")]
        public void WhenIchangetheCategoryto(string args1)
        {
            _warehouse.Pending();
        }

        [Then(@"the Category of the warehouse should be ""(.*)""")]
        public void ThentheCategoryofthewarehouseshouldbe(string args1)
        {
            _warehouse.Pending();
        }

        [Then(@"It should fail")]
        public void ThenItshouldfail()
        {
            _warehouse.Pending();
        }

        [Then(@"It should fail and not let me change")]
        public void ThenItshouldfailandnotletmechange()
        {
            _warehouse.Pending();
        }

    }
}