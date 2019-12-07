using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;

namespace DirectStar
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        readonly Mutex mutex = new Mutex(false, "MainMutex");
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!mutex.WaitOne(500, false))
            {
                Environment.Exit(0);
            }
        }
    }
}
