using AutoNotify;
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

        //[AutoNotify]
        private static string staticstring;

        //[AutoNotify]
        private readonly string readonlystring;

        public string Staticstring
        {
            get => staticstring;
            set
            {
                if (global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(staticstring, value))
                    return;

                this.PropertyChanging?.Invoke(this, new global::System.ComponentModel.PropertyChangingEventArgs("Staticstring"));

                staticstring = value;

                this.PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs("Staticstring"));
            }
        }

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
