using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections.Generic;
using Golden.Utility;
using System.Linq;

namespace Golden.Mvvm
{
    internal struct RuleMap
    {
        public Func<bool> Predicate { get; set; }
        public string ErrorMessageFormat { get; set; }
        public string PropertyName { get; set; }

        public RuleMap(Func<bool> predicate, string errorMessageFormat = null, string propertyName = "")
        {
            this.Predicate = predicate;
            this.ErrorMessageFormat = errorMessageFormat;
            this.PropertyName = (propertyName ?? "");
        }
    }
#if NET45
    public abstract class ViewModelBase : ObservableObject, INotifyDataErrorInfo
    {
        private readonly object threadLock = new object();
        private EventHandler<DataErrorsChangedEventArgs> _ErrorChanged;
        private readonly List<RuleMap> _Rules = new List<RuleMap>();

        public virtual bool HasErrors
        {
            get { return InternalHasErrors(); }
        }
        #region INotifyDataErrorInfo
        System.Collections.IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            return GetErrors(propertyName);
        }
        event EventHandler<DataErrorsChangedEventArgs> INotifyDataErrorInfo.ErrorsChanged
        {
            add { _ErrorChanged += value; }
            remove { _ErrorChanged -= value; }
        }
        #endregion

        private void RaiseErrorsChanged(string propertyName)
        {
            _ErrorChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
        public virtual IEnumerable<string> GetErrors()
        {
            return GetErrors("");
        }
        protected IEnumerable<string> GetErrors<T>(Expression<Func<T>> property)
        {
            return GetErrors(Utilities.GetMemberName(property));
        }
        protected virtual IEnumerable<string> GetErrors(string propertyName)
        {
            if (_Rules.Count > 0)
            {
                if (propertyName == null) propertyName = "";
                IEnumerable<RuleMap> rules = _Rules;
                if (!propertyName.IsNullOrEmpty())
                    rules = rules.Where(r => r.PropertyName.EqualsOrdinal(propertyName));
                foreach (var rule in rules)
                {
                    if (!rule.Predicate.Invoke())
                        yield return string.Format(rule.ErrorMessageFormat, propertyName, TypeHelper.GetMemberValue(rule.PropertyName, this));
                }
            }
            yield break;
        }
        protected void Validate<T>(Expression<Func<T>> property)
        {
            Validate(Utilities.GetMemberName(property));
        }
        protected void Validate()
        {
            Validate("");
        }
        protected virtual void Validate(string propertyName)
        {
            if (_Rules.Count == 0) return;

            lock (threadLock)
            {
                if (propertyName == null) propertyName = "";
                IEnumerable<RuleMap> rules = _Rules;
                if (!propertyName.IsNullOrEmpty())
                    rules = rules.Where(r => r.PropertyName.EqualsOrdinal(propertyName));

                bool errorChange = false;
                foreach (var rule in rules)
                {
                    if (!rule.Predicate.Invoke())
                        errorChange = true;
                }
                if (errorChange) RaiseErrorsChanged(propertyName);
            }
        }
        private bool InternalHasErrors()
        {
            return (_Rules.Count > 0 && _Rules.Any(r => !r.Predicate.Invoke()));
        }
        protected void AddRule(Func<bool> predicate)
        {
            AddRule(predicate, "");
        }
        protected void AddRule(Func<bool> predicate, string errorMessageFormat)
        {
            AddRule(predicate, errorMessageFormat, null);
        }
        protected void AddRule(Func<bool> predicate, Expression<Func<object>> property)
        {
            AddRule(predicate, "", property);
        }
        protected void AddRule(Func<bool> predicate, string errorMessageFormat, Expression<Func<object>> property)
        {
            var propertyName = (property != null ? Utilities.GetMemberName(property) : "");
            _Rules.Add(new RuleMap(predicate, errorMessageFormat, propertyName));
        }
    }
#else
    public abstract class ViewModelBase : ObservableObject, IDataErrorInfo
    {
        private readonly object threadLock = new object();
        private readonly List<RuleMap> _Rules = new List<RuleMap>();

        public virtual bool HasErrors
        {
            get { return InternalHasErrors(); }
        }
    #region IDataErrorInfo
        string IDataErrorInfo.Error
        {
            get
            {
                //var error = string.Join(Environment.NewLine, GetErrors());
                var error = GetErrors().FirstOrDefault();
                return error;
            }
        }
        string IDataErrorInfo.this[string propertyName]
        {
            get
            {
                //var error = string.Join(Environment.NewLine, GetErrors());
                var error = GetErrors(propertyName).FirstOrDefault();
                return error;
            }
        }
    #endregion

        private bool InternalHasErrors()
        {
            return (_Rules.Count > 0 && _Rules.Any(r => !r.Predicate.Invoke()));
        }
        protected void AddRule(Func<bool> predicate)
        {
            AddRule(predicate, "");
        }
        protected void AddRule(Func<bool> predicate, string errorMessageFormat)
        {
            AddRule(predicate, errorMessageFormat, null);
        }
        protected void AddRule(Func<bool> predicate, Expression<Func<object>> property)
        {
            AddRule(predicate, "", property);
        }
        protected void AddRule(Func<bool> predicate, string errorMessageFormat, Expression<Func<object>> property)
        {
            var propertyName = (property != null ? Utilities.GetMemberName(property) : "");
            _Rules.Add(new RuleMap(predicate, errorMessageFormat, propertyName));
        }
        public virtual IEnumerable<string> GetErrors()
        {
            return GetErrors("");
        }
        protected IEnumerable<string> GetErrors<T>(Expression<Func<T>> property)
        {
            return GetErrors(Utilities.GetMemberName(property));
        }
        protected virtual IEnumerable<string> GetErrors(string propertyName)
        {
            if (_Rules.Count > 0)
            {
                if (propertyName == null) propertyName = "";
                IEnumerable<RuleMap> rules = _Rules;
                if (!propertyName.IsNullOrEmpty())
                    rules = rules.Where(r => r.PropertyName.EqualsOrdinal(propertyName));
                foreach (var rule in rules)
                {
                    if (!rule.Predicate.Invoke())
                        yield return string.Format(rule.ErrorMessageFormat, propertyName, TypeHelper.GetMemberValue(rule.PropertyName, this));
                }
            }
            yield break;
        }
    }
#endif

    //TODO: Remove these lines of code(using InteractionRequest<IMessageBox, IDialog, IFileDialog>)
    //public abstract class ViewModelBase<TView> : ViewModelBase where TView : IView
    //{
    //    private readonly TView _View;

    //    public virtual TView View
    //    {
    //        get { return _View; }
    //    }

    //    public ViewModelBase() : this(default(TView))
    //    {
    //    }
    //    public ViewModelBase(TView view)
    //    {
    //        _View = view;
    //        if (view != null) view.DataContext = this;
    //    }
    //}
}
