using BlubbiChanged;
using System;

namespace ConsoleApp1
{
    partial class Program
    {
        [AutoNotify]
        private string blubber;

        /// <summary>
        /// HEH <see cref="miro"/> <see langword="true"/>
        /// </summary>
        [AutoNotify(PropertyName = "blubb")]
        private string miro;

        [AutoNotify]
        private static string staticstring;

        //[AutoNotify]
        //private readonly string readonlystrings;

        //[AutoNotify]
        //private string _;

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
