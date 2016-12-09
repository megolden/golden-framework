using Golden.Attributes;
using Golden.Win.Mvvm;
using Golden.Win.Mvvm.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Golden.Win.Sample.Applications
{
    [Export(typeof(IMainWindowView))]
    public class MainWindowViewModel : ViewModelBase<IMainWindowView>
    {
        public virtual string Title { get; set; }
        public virtual string Name { get; set; }
        public virtual DateTime? BirthDate { get; set; }
        public virtual byte? Age
        {
            get
            {
                if (!BirthDate.HasValue) return null;
                return (byte)(DateTime.Today.Year - BirthDate.Value.Year);
            }
        }
        public virtual ICollection<float> Marks { get; set; }

        public MainWindowViewModel(IMainWindowView view) : base(view)
        {
        }
        public void Save()
        {
            Debug.WriteLine("Saved");
        }
        protected bool CanSave()
        {
            return !HasErrors;
        }
        protected void BirthDateChanging()
        {
            Debug.WriteLine("'BirthDate' Changing...");
        }
        protected void BirthDateChanged()
        {
            Debug.WriteLine("'BirthDate' Changed...");
        }
        protected void OnCreated()
        {
            //Add random marks.
            var random = new Random();
            Enumerable.Repeat(0, 10).ForEach(i => Marks.Add(random.Next(21)));
        }
        private static void OnMapping(ViewModelConfiguration<MainWindowViewModel> config)
        {
            config.OnCreated(m => m.OnCreated);
            config.Properties(m => new { m.Title, m.Name, m.BirthDate });

            config.Property(m => m.Title)
                .HasDefaultValue("Student Information");
            config.Property(m => m.Marks)
                .HasNewInstance(typeof(ObservableCollection<float>));
            config.Property(m => m.BirthDate)
                .HasDependency(m => m.Age)
                .HasRule(value => value.HasValue, "'{0}' not specified")
                .HasDependencyRule(vm => !vm.Age.HasValue || vm.Age > 0, "'{0}' has invalid value");
            config.Property(m => m.Name)
                .HasRule(value => !value.IsNullOrWhiteSpace(), "'{0}' not specified");
            config.Property(m => m.BirthDate)
                .OnChanging(m => m.BirthDateChanging)
                .OnChanged(m => m.BirthDateChanged);

            config.Command(m => m.Save).CanExecute(m => m.CanSave);
        }
    }
}
