using Golden.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Golden.Win.Sample.Components
{
    public interface IApplication
    {
        IView MainView { get; set; }

        void Shutdown();
        bool HandleException(Exception exception);
    }
}
