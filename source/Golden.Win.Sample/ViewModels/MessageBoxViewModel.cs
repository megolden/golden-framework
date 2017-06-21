using Golden.Mvvm.Configuration.Annotations;
using Golden.Win.Sample.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Golden.Win.Sample.ViewModels
{
    public class MessageBoxViewModel : AppViewModel<IWindowView>
    {
        public MessageBoxViewModel():this(null)
        {
        }
        public MessageBoxViewModel(IWindowView view) : base(view)
        {
        }

        [Command]
        public void Result(MessageBoxResult result)
        {
            Debug.WriteLine("Result command body...");
        }
    }
}
