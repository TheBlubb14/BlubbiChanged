using AutoNotify;
using System;

namespace ConsoleApp1
{
    partial class Program
    {
        [AutoNotify]
        private string blubberua;

        /// <summary>
        /// HEH
        /// </summary>
        [AutoNotify]
        private string miro;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            
        }

        private void a()
        {
            var a = miro.Length == 0;
            miro = "aaaa";
        }
    }
}
