using Autofac;
using Golden.Win.Sample.Components;
using Golden.Win.Sample.ViewModels;
using Golden.Win.Sample.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Golden.Win.Sample
{
    public class Startup
    {
        public void Run()
        {
            Run(new string[0]);
        }
        public void Run(string[] args)
        {
            //Dependency Injections
            DIConfig.Register();

            //Common WindowsApplication initializations.
            System.Windows.Forms.Application.EnableVisualStyles();
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            //Handle exceptions.
            Application.Current.DispatcherUnhandledException += (s, ea) =>
                ea.Handled = DIConfig.Injector.Resolve<IErrorLogger>().Handle(ea.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, ea) =>
                DIConfig.Injector.Resolve<IErrorLogger>().Handle(ea.ExceptionObject as Exception);

            //Start application
            Start(args);
        }
        private void Start(string[] args)
        {
            var viewModel = DIConfig.Injector.Resolve<MainPageViewModel>();
            var view = DIConfig.Injector.Resolve<Shell>(viewModel);
            Application.Current.MainWindow = view;
            Application.Current.MainWindow.Show();
        }
    }
}
