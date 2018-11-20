using Golden.Annotations;
using Golden.Mvvm;
using Golden.Mvvm.Configuration;
using Golden.Mvvm.Configuration.Annotations;
using Golden.Mvvm.Interactivity;
using Golden.Win.Sample.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Golden.Win.Sample.Services;
using Golden.Win.Sample.Components;

namespace Golden.Win.Sample.ViewModels
{
    public class MainPageViewModel : AppViewModel
    {
        private readonly IModalService svcModal;
        private readonly IApplicationService svcApp;

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

        public MainPageViewModel(IModalService svcModal, IApplicationService svcApp)
        {
            this.svcModal = svcModal;
            this.svcApp = svcApp;

            AddRule(() => Age.HasValue, "'{0}' not specified", () => Age);
            AddRule(() => Age > 0, "'{0}' has invalid value '{1}'", () => Age);
            AddRule(() => !Name.IsNullOrWhiteSpace(), "'{0}' not specified", () => Name);
        }
        public void Save()
        {
            MessageBox.Show(this.Title);
            //svcModal.ShowMessageBox(this.Title, "Your data saved successfully!", MessageBoxButton.OK);
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
        public void OnClosing(CancelEventArgs args)
        {
            var msgResult = svcModal.ShowMessageBox("Exit Confirm", "Are you sure you want to exit app ?", MessageBoxButton.YesNo);
            args.Cancel = (msgResult != true);
        }
        public void OnPreviewKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Q && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                args.Handled = true;
                svcApp.Shutdown();
            }
        }
        protected virtual void OnViewModelRegister(ViewModelConfiguration<MainPageViewModel> config)
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
            config.Command<CancelEventArgs>(() => OnClosing);
        }
    }
}
