using Golden.Win.Sample.Components;
using Golden.Win.Sample.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Golden.Win.Sample.Services
{
    public class ModalService : IModalService
    {
        private readonly Func<ViewModels.MessageBoxViewModel> vmMessageBoxCreator;

        public ModalService(Func<ViewModels.MessageBoxViewModel> vmMessageBoxCreator)
        {
            this.vmMessageBoxCreator = vmMessageBoxCreator;
        }

        public bool? ShowMessageBox(string title, string message, MessageBoxButton buttons)
        {
            var vm = vmMessageBoxCreator.Invoke();
            vm.Title = title;
            vm.Message = message;
            vm.Buttons = buttons;
            return vm.View.ShowDialog();
        }
    }
}
