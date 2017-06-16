using Golden.Annotations;
using Golden.Mvvm;
using Golden.Mvvm.Configuration;
using Golden.Mvvm.Configuration.Annotations;
using Golden.Mvvm.Interactivity;
using Golden.Win.Sample.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Golden.Win.Sample.Applications
{
    public class MainWindowViewModel : AppViewModel<IMainWindowView>
    {
        public override string Title
        {
            get {return base.Title;}
            set{base.Title = value;}
        }
        public virtual string Name { get; set; }
        public virtual DateTime? BirthDate { get; set; }
        public virtual short? Age
        {
            get
            {
                if (!BirthDate.HasValue) return null;
                return (short)(DateTime.Today.Year - BirthDate.Value.Year);
            }
        }
        public ObservableCollection<float> Marks { get; } = new ObservableCollection<float>();

        public MainWindowViewModel() : this(null)
        {
        }
        public MainWindowViewModel(IMainWindowView view) : base(view)
        {
            AddRule(() => Age.HasValue, "'{0}' not specified", () => Age);
            AddRule(() => Age > 0, "'{0}' has invalid value '{1}'", () => Age);
            AddRule(() => !Name.IsNullOrWhiteSpace(), "'{0}' not specified", () => Name);
        }
        public void Save()
        {
            var vm = MvvmHelper.CreateViewModel<MessageBoxViewModel>(new Views.MessageBoxView());
            vm.View.ShowDialog();
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
        public void TMouseDown(MouseButtonEventArgs args)
        {
            args.Handled = true;
        }
        public void TMouseUp(object sender, MouseButtonEventArgs args)
        {
            args.Handled = true;
        }
        protected virtual void OnViewModelRegister(ViewModelConfiguration<MainWindowViewModel> config)
        {
            base.OnViewModelRegister(config);

            config.OnInitilize(() => OnCreated);

            config.Property(() => new { Name, BirthDate });
            config.Property(() => Title)
                .HasDefaultValue("Student Information");
            config.Property(() => BirthDate)
                .HasDependency(() => Age)
                .OnChanging(() => BirthDateChanging)
                .OnChanged(() => BirthDateChanged);

            config.Command(() => Save).CanExecute(() => CanSave);
            config.Command<MouseButtonEventArgs>(() => TMouseDown);
        }
    }
}
