using System;
using System.Threading;

namespace MinimalTest
{
    public class Program
    {
        public static void Main()
        {
            while (true)
            {
                Console.WriteLine("Hello from nanoFramework!");
                Thread.Sleep(2000);
            }
        }
    }
}
