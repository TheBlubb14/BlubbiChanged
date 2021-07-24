using AutoNotify;
using System;

namespace ConsoleApp1
{
    partial class Program
    {
        [AutoNotify]
        private string blubber;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
