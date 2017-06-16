using Golden.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Golden.Win.Sample.Components
{
    public interface IWindowView : IView
    {
        string Title { get; set; }
        bool? DialogResult { get; set; }

        void Show();
        bool? ShowDialog();
        void Close();
    }
}
