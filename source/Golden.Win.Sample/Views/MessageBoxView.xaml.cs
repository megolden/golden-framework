using Golden.GoldenExtensions;
using Golden.Win.Sample.Components;
using Golden.Win.Sample.ViewModels;
using Golden.Win.Sample.Views.Interfaces;
using Golden.Win.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Golden.Win.Sample.Views
{
    public partial class MessageBoxView : Window, IMessageBoxView
    {
        public MessageBoxView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as MessageBoxViewModel;
            OKButton.Visibility = (vm.Buttons.IsIn(MessageBoxButton.OK, MessageBoxButton.OKCancel) ? Visibility.Visible : Visibility.Collapsed);
            YesButton.Visibility = (vm.Buttons.IsIn(MessageBoxButton.YesNo, MessageBoxButton.YesNoCancel) ? Visibility.Visible : Visibility.Collapsed);
            NoButton.Visibility = (vm.Buttons.IsIn(MessageBoxButton.YesNo, MessageBoxButton.YesNoCancel) ? Visibility.Visible : Visibility.Collapsed);
            CancelButton.Visibility = (vm.Buttons.IsIn(MessageBoxButton.OKCancel, MessageBoxButton.YesNoCancel) ? Visibility.Visible : Visibility.Collapsed);
            ButtonsPanel.Children.OfType<Button>().Where(btn => btn.IsVisible).ToList().ForEach((btn, i) => btn.Margin = new Thickness((i == 0 ? 0 : 5), 0, 0, 0));
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowHelper.RemoveMaximizeButton(this.GetHandle());
            WindowHelper.RemoveMinimizeButton(this.GetHandle());
            WindowHelper.RemoveRestoreMenuItem(this.GetHandle());
            WindowHelper.RemoveResizeMenuItem(this.GetHandle());
            WindowHelper.RemoveSeparatorMenuItem(this.GetHandle());
            WindowHelper.RemoveIcon(this.GetHandle());
        }
        bool? IWindowView.ShowDialog()
        {
            var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (owner != null)
                this.Owner = owner;
            return this.ShowDialog();
        }
    }
}
