using Golden.GoldenExtensions;
using Golden.Mvvm.Configuration.Annotations;
using Golden.Win.Sample.Components;
using Golden.Win.Sample.Views.Interfaces;
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
    public class MessageBoxViewModel : AppViewModel<IMessageBoxView>
    {
        [Property]
        public virtual string Message { get; set; }
        [Property]
        public virtual MessageBoxButton Buttons { get; set; } = MessageBoxButton.OK;

        public MessageBoxViewModel(IMessageBoxView view) : base(view)
        {
        }

        [Command]
        public void Result(MessageBoxResult result)
        {
            if (result.IsIn(MessageBoxResult.None, MessageBoxResult.Cancel))
            {
                this.View.DialogResult = null;
                this.View.Close();
            }
            else
            {
                this.View.DialogResult = result.IsIn(MessageBoxResult.OK, MessageBoxResult.Yes);
            }
        }
    }
}
