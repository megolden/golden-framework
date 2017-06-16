using Golden.Utility;
using Golden.Mvvm;
using Golden.Win.Sample.Applications;
using Golden.Win.Sample.Components;
using Golden.Win.Sample.Views;
using System;
using System.Windows;
using System.Windows.Input;

namespace Golden.Win.Sample
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //Register view model configurations.
            //MvvmHelper.RegisterConfiguration<MainWindowViewModel>(config =>
            //{
            //    config.OnCreated("OnCreated");

            //    config.Property(m => new { m.Title, m.Name, m.BirthDate });
            //    config.Property(m => m.Title)
            //        .HasDefaultValue("Student Information");
            //    config.Property(m => m.BirthDate)
            //        .HasDependency(m => m.Age)
            //        .OnChanging("BirthDateChanging")
            //        .OnChanged("BirthDateChanged");

            //    config.Command(m => m.Save).CanExecute("CanSave");
            //    config.Command<MouseButtonEventArgs>(m => m.TMouseDown);
            //});

            //Common WindowsApplication initializations.
            System.Windows.Forms.Application.EnableVisualStyles();
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;

            //Catch not handled exceptions.
            this.DispatcherUnhandledException += (s, ea) => { ea.Handled = HandleException(ea.Exception); };
            AppDomain.CurrentDomain.UnhandledException += (s, ea) => HandleException(ea.ExceptionObject as Exception);

            Start(e.Args);
        }
        private void Start(string[] args)
        {
            var viewModel = MvvmHelper.CreateViewModel<MainWindowViewModel>(new MainWindow());
            this.MainWindow = viewModel.View as Window;
            viewModel.View.Show();
        }
        public bool HandleException(Exception ex)
        {
            if (ex == null) return true;

            //Write code here
            return true;
        }
    }
}
