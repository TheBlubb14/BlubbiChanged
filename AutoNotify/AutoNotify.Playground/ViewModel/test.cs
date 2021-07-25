using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoNotify;

namespace AutoNotify.Playground.ViewModel
{
    public partial class test
    {
        /// <summary>
        /// blabla
        /// </summary>
        [AutoNotify]
        private string titaleaaa;
        [AutoNotify]
        private string miro;



        private void a()
        {
            var a = miro.Length == 0;
            miro = "aaaa";
        }
    }
}
