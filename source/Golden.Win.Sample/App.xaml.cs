using Golden.Utility;
using Golden.Win.Mvvm;
using Golden.Win.Sample.Applications;
using Golden.Win.Sample.Components;
using Golden.Win.Sample.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Golden.Win.Sample
{
	public partial class App : Application, IApplication
	{
        public ICommand ShutdownCommand { get; private set; }
        public new static IApplication Current { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
		{
            Current = this;
            ShutdownCommand = new DelegateCommand<object>(p => Shutdown(Utilities.Convert<int?>(p).GetValueOrDefault(0)));

            base.OnStartup(e);

            //Common WindowsApplication initializations.
			System.Windows.Forms.Application.EnableVisualStyles();
			System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
			this.ShutdownMode = ShutdownMode.OnMainWindowClose;

            //Handles dependency injection

            //Handle not handled exceptions.
            this.DispatcherUnhandledException += (s, ea) => { HandleException(ea.Exception); ea.Handled = true; };
			AppDomain.CurrentDomain.UnhandledException += (s, ea) => HandleException(ea.ExceptionObject as Exception);

			Start(e.Args);
		}
        private void Start(string[] args)
        {
            var viewModel = MvvmHelper.CreateViewModel<MainWindowViewModel>(new MainWindow());
            this.MainWindow = viewModel.View as Window;
            viewModel.View.Show();
        }
        public void HandleException(Exception ex)
        {
            if (ex == null) return;

            //Write code here
        }
    }
}
