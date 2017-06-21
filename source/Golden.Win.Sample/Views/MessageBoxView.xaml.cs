using Golden.Win.Sample.Components;
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
    public partial class MessageBoxView : Window, IWindowView
    {
        public MessageBoxView()
        {
            InitializeComponent();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowHelper.RemoveMaximizeButton(this.GetHandle());
            WindowHelper.RemoveMinimizeButton(this.GetHandle());
        }
    }
}
