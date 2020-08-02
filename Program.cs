using System;

namespace BeaterText
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                switch (args[0].ToLower())
                {
                    case "-d":
                        TextParser p = new TextParser(args[1], args[2]);
                        break;
                    case "-m":
                        TextLexer l = new TextLexer(args[1], args[2]);
                        break;
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine(usage);
            }
        }

        private static string usage = $"Bah humbug";
    }
}
