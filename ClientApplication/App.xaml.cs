using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ClientApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Type[] extraAllowedTypes = new Type[]
            {
                typeof(WcfServiceLibrary.ComplexInteger),
                typeof(WcfServiceLibrary.ComplexReal)
            };
            AppDomain.CurrentDomain.SetData("System.Data.DataSetDefaultAllowedTypes", extraAllowedTypes);
        }
    }
}
