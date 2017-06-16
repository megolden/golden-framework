using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Golden
{
    /*
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq.Expressions;

	public class ObservableObject : INotifyPropertyChanged, IDataErrorInfo
	{
		internal const string RaisePropertyChangedMethodName = nameof(ObservableObject.RaisePropertyChanged);

		private struct RuleBind
		{
			public readonly string PropertyName;
			public readonly Func<bool> ValidateFunction;
			public readonly string ErrorMessage;

			public RuleBind(string propertyName, Func<bool> validateFunction, string errorMessage)
			{
				this.PropertyName = propertyName;
				this.ValidateFunction = validateFunction;
				this.ErrorMessage = errorMessage;
			}
		}

		private event PropertyChangedEventHandler _PropertyChangedHandler;
		private readonly List<RuleBind> _RuleMap = new List<RuleBind>();

		#region IDataErrorInfo
		string IDataErrorInfo.Error
		{
			get {
				string errorMessage;
				if (Validate(out errorMessage)) return "";
				return (errorMessage ??"");
			}
		}
		string IDataErrorInfo.this[string propertyName]
		{
			get
			{
				string errorMessage = null;
				if (Validate(propertyName, out errorMessage)) return "";
				return (errorMessage ?? "");
			}
		} 
		#endregion

		protected void RaisePropertyChanged()
		{
			RaisePropertyChanged("");
		}
		protected virtual void RaisePropertyChanged(string propertyName)
		{
			OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		}
		protected void RaisePropertyChanged<T>(Expression<Func<T>> property)
		{
			var memExp = property.Body as MemberExpression;
			if (memExp == null) throw new ArgumentOutOfRangeException();
			var propertyName = memExp.Member.Name;
			RaisePropertyChanged(propertyName);
		}
		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (_PropertyChangedHandler != null) _PropertyChangedHandler(this, e);
		}
		public bool Validate()
		{
			string errorMessage;
			return Validate(out errorMessage);
		}
		public virtual bool Validate(out string errorMessage)
		{
			errorMessage = "";
			if (_RuleMap.Count == 0) return true;
			foreach (var rb in _RuleMap)
			{
				if (!rb.ValidateFunction.Invoke())
				{
					errorMessage = rb.ErrorMessage;
					return false;
				}
			}
			return true;
		}
		protected bool Validate<T>(Expression<Func<T>> property)
		{
			string errorMessage;
			return Validate(property, out errorMessage);
		}
		protected bool Validate<T>(Expression<Func<T>> property, out string errorMessage)
		{
			return Validate(Utility.Utilities.GetMemberName(property), out errorMessage);
		}
		protected virtual bool Validate(string propertyName, out string errorMessage)
		{
			errorMessage = "";
			if (_RuleMap.Count == 0) return true;
			foreach (var rb in _RuleMap.Where(i=>i.PropertyName.EqualsOrdinal(propertyName)))
			{
				if (!rb.ValidateFunction.Invoke())
				{
					errorMessage = rb.ErrorMessage;
					return false;
				}
			}
			return true;
		}
		protected virtual void AddRule<T>(Expression<Func<T>> property, Func<bool> validateFunction, string errorMessage)
		{
			_RuleMap.Add(new RuleBind(Utility.Utilities.GetMemberName(property), validateFunction, errorMessage));
		}
		protected virtual int RemoveRules<T>(Expression<Func<T>> property)
		{
			if (_RuleMap.Count == 0) return 0;
			var propertyName = Utility.Utilities.GetMemberName(property);
			return _RuleMap.RemoveAll(i => i.PropertyName.EqualsOrdinal(propertyName));
		}
		protected virtual void RemoveAllRules()
		{
			_RuleMap.Clear();
		}

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { _PropertyChangedHandler += value; }
			remove { _PropertyChangedHandler -= value; }
		}
	}
    */

    public abstract class ObservableObject : INotifyPropertyChanged
    {
        private event PropertyChangedEventHandler _PropertyChanged;

        //protected virtual bool IsTouched { get; set; }

        protected void RaisePropertyChanged()
        {
            RaisePropertyChanged("");
        }
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
        protected void RaisePropertyChanged<T>(Expression<Func<T>> property)
        {
            var propertyName = Utility.Utilities.GetMember(property)?.Name;

            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentOutOfRangeException(nameof(property));

            RaisePropertyChanged(propertyName);
        }
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            //this.IsTouched = true;
            _PropertyChanged?.Invoke(this, e);
        }
        //protected virtual void Set<T>(Expression<Func<T>> property, T value)
        //{
        //	var memberExp = property.Body as MemberExpression;
        //	if (memberExp == null) memberExp = (MemberExpression)((UnaryExpression)property.Body).Operand;
        //	var obj = Expression.Lambda<Func<ObservableObject>>(memberExp.Expression).Compile().Invoke();
        //	Utility.TypeHelper.SetMemberValue(memberExp.Member, value, obj);
        //	obj.RaisePropertyChanged(memberExp.Member.Name);
        //}

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _PropertyChanged += value; }
            remove { _PropertyChanged -= value; }
        }
    }
}
