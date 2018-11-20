using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Golden.Win.Sample.Services
{
    public interface IModalService
    {
        bool? ShowMessageBox(string title, string message, MessageBoxButton buttons);
    }
}
