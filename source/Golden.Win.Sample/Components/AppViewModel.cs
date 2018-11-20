using Golden.Mvvm;
using Golden.Mvvm.Configuration;
using Golden.Mvvm.Configuration.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Golden.Win.Sample.Components
{
    public abstract class AppViewModel : ViewModelBase
    {
        public virtual string Title { get; set; }

        protected virtual void OnViewModelRegister<T>(ViewModelConfiguration<T> config) where T : AppViewModel
        {
            config.Property(() => Title)
                .HasDefaultValue("");
        }
    }
}
