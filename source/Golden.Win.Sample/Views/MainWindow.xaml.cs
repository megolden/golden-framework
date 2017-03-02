using System;
using Golden.Annotations;
using Golden.Win.Mvvm;
using System.Globalization;
using System.Windows;
using Golden.Win.Sample.Applications;

namespace Golden.Win.Sample.Views
{
	[Export(typeof(IMainWindowView))]
	public partial class MainWindow : Window, IMainWindowView
	{
		public MainWindow()
		{
			InitializeComponent();
		}
    }
}
