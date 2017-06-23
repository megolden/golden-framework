using Autofac;
using Golden.Mvvm;
using Golden.Win.Sample.ViewModels;
using Golden.Win.Sample.Components;
using Golden.Win.Sample.Views;
using System;
using System.Windows;

namespace Golden.Win.Sample
{
    public partial class App : Application, IApplication
    {
        public IView MainView
        {
            get { return (this.MainWindow as IView); }
            set { this.MainWindow = value as Window; }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //Dependency Injections
            DIConfig.Register();

            //Common WindowsApplication initializations.
            System.Windows.Forms.Application.EnableVisualStyles();
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;

            //Not handled exceptions.
            this.DispatcherUnhandledException += (s, ea) =>
                ea.Handled = DIConfig.Injector.Resolve<IApplication>().HandleException(ea.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, ea) => 
                DIConfig.Injector.Resolve<IApplication>().HandleException(ea.ExceptionObject as Exception);

            //Start application
            Start(e.Args);
        }
        private void Start(string[] args)
        {
            var viewModel = DIConfig.Injector.Resolve<MainWindowViewModel>();
            DIConfig.Injector.Resolve<IApplication>().MainView = viewModel.View;
            viewModel.View.Show();
        }
        public bool HandleException(Exception exception)
        {
            if (exception == null) return true;

            //Write code here
            return true;
        }
    }
}
