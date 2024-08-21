namespace ReqnrollDemo
{
    public class DemoFunctionality
    {
        public string Name1 { get; set; }

        public string Name2 { get; set; }

        public string CombineName() {
            if (Name1.ToLower() == Name2.ToLower())
                return Name1;
            return Name1 +  " " + Name2; 
        }

        

    }
}
