using System;
using Golden.Annotations;
using System.Globalization;
using System.Windows;
using Golden.Win.Sample.Views.Interfaces;

namespace Golden.Win.Sample.Views
{
	public partial class MainWindow : Window, IMainWindowView
	{
		public MainWindow()
		{
			InitializeComponent();
		}
    }
}
