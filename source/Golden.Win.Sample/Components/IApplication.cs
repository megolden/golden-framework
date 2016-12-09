using System;
using Golden.Win.Mvvm;
using System.Windows.Input;

namespace Golden.Win.Sample.Components
{
    public interface IApplication
	{
        ICommand ShutdownCommand { get; }

        void Shutdown(int exitCode);
        void HandleException(Exception ex);
    }
}
