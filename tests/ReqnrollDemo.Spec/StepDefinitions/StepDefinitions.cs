using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReqnrollDemo.Spec.StepDefinitions
{
    [Binding]
    public class StepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;
        private DemoFunctionality _demoFunctionality;

        public StepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            _demoFunctionality = new DemoFunctionality();
        }

        [Given(@"firstname is ""(.*)""")]
        public void GivenFirstnameIs(string fistname)
        {
            _demoFunctionality.Name1 = fistname;
        }

        [Given(@"lastname is ""(.*)""")]
        public void GivenLastnameIs(string lastname)
        {
            _demoFunctionality.Name2 = lastname;
        }

        [When(@"the two names are combined")]
        public void Whenthetwonamesarecombined()
        {
            _scenarioContext.Add("combined", _demoFunctionality.CombineName());
        }

        [Then(@"the result should be ""(.*)""")]
        public void Thentheresultshouldbe(string args1)
        {
            var result = (string)(_scenarioContext["combined"]);
            Assert.Equal(args1, result);
        }

    }
}