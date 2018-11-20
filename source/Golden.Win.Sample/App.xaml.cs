using Autofac;
using Golden.Mvvm;
using Golden.Win.Sample.ViewModels;
using Golden.Win.Sample.Views;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Data;
using Golden.GoldenExtensions;

namespace Golden.Win.Sample
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            new Startup().Run(e.Args);
        }
    }
}
