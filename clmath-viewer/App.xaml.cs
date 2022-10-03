using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace clmath.viewer
{
    public partial class App : Application
    {
        public static string Func = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            Func = string.Join(" ", e.Args);
            base.OnStartup(e);
        }
    }
}