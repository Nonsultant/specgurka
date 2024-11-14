using System.ComponentModel.Design;

namespace SpecGurka
{
    public class UIHelper
    {
        public void PrintError(string text)
        {
            Console.Write(DateTime.Now);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(" ERROR: ");
            Console.ResetColor();
            Console.WriteLine(text);
        }

        public void PrintCancel()
        {
            Console.Write(DateTime.Now);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" CANCELLING TEST.");
            Console.ResetColor();
        }

        public void PrintCancel(string text)
        {
            Console.Write(DateTime.Now);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" { text }");
            Console.ResetColor();
        }

        public void PrintOk(string text)
        {
            Console.Write(DateTime.Now);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" OK: ");
            Console.ResetColor();
            Console.WriteLine(text);
        }

        public void PrintWarning(string text)
        {
            Console.Write(DateTime.Now);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" WARNING: ");
            Console.ResetColor();
            Console.WriteLine(text);
        }

        public void PrintReading(string text1, string text2)
        {
            Console.Write($"\n\n{DateTime.Now}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($" READING {text1}: ");

            Console.WriteLine(text2);
            Console.ResetColor();
        }

        public void PrintTitle(string text1)
        {
            Console.Write($"\n\n{DateTime.Now}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($" {text1}");
            Console.ResetColor();
        }
    }
}
