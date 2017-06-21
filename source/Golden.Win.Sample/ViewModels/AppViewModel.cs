using Golden.Mvvm;
using Golden.Mvvm.Configuration;
using Golden.Mvvm.Configuration.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Golden.Win.Sample.ViewModels
{
    public abstract class AppViewModel : ViewModelBase
    {
    }
    public abstract class AppViewModel<TView> : ViewModelBase<TView> where TView : IView
    {
        public virtual string Title { get; set; }

        public AppViewModel() : this(default(TView))
        {
        }
        public AppViewModel(TView view) : base(view)
        {
        }
        protected virtual void OnViewModelRegister<T>(ViewModelConfiguration<T> config) where T : AppViewModel<TView>
        {
            config.Property(() => Title)
                .HasDefaultValue("");
        }
    }
}
