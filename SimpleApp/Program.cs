// SimpleApp/Program.cs
using System;

namespace SimpleApp
{
    public class Calculator
    {
        public int Add(int a, int b) => a + b;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var calc = new Calculator();
            Console.WriteLine($"2 + 3 = {calc.Add(2, 3)}");
        }
    }
}