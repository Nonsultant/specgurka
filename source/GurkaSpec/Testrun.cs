using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpecGurka.GurkaSpec
{
    public class Testrun
    {
        public string TestName { get; set; }

        

        private TimeSpan testDuration;
        public string TestDuration
        {
            get
            {
                testDuration = TimeSpan.Zero;
                foreach (var product in Products)
                {
                    testDuration = testDuration.Add(product.TestDurationTime);
                }
                return testDuration.ToString("G");
            }
            set { }
        }

        public List<Product> Products { get; set; } = new List<Product>();

        public Product GetProduct(string name)
        {
            var product = Products.FirstOrDefault(f => f.Name == name);
            return product;
        }
    }
}
