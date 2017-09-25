using Golden.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Golden.Mvvm.Configuration
{
    internal interface ICommandConfiguration
    {
        MethodInfo Method { get; }
        string Name { get; }
        MethodInfo CanExecuteMethod { get; }
        Type FirstParameterType { get; }
    }
    internal interface IPropertyConfiguration
    {
        PropertyInfo BaseProperty { get; }
        object DefaultValue { get; }
        bool HasDefaultValue { get; }
        MethodInfo ChangingMethod { get; }
        MethodInfo ChangedMethod { get; }
        ICollection<string> DependencyProperties { get; }
    }
    internal interface IViewModelConfiguration
    {
        Type Type { get; }
        MethodInfo OnCreatedMethod { get; }
        ICollection<IPropertyConfiguration> Properties { get; }
        ICollection<ICommandConfiguration> Commands { get; }
    }
    public class CommandConfiguration<T> : ICommandConfiguration
    {
        private readonly MethodInfo _Method;
        private string _Name;
        private MethodInfo _CanExecuteMethod;
        private readonly Type _FirstParameterType = null;

        public CommandConfiguration(MethodInfo method)
        {
            _Method = method;
            _Name = string.Concat(method.Name, "Command");
            _FirstParameterType = _Method.GetParameters().FirstOrDefault()?.ParameterType;
        }
        public CommandConfiguration<T> HasCommandName(string name)
        {
            _Name = name;
            return this;
        }
        public CommandConfiguration<T> CanExecute(string methodName)
        {
            var memberInfo = typeof(T).GetMethod(methodName, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public);
            _CanExecuteMethod = memberInfo;
            return this;
        }
        public CommandConfiguration<T> CanExecute(Expression<Func<T, Func<bool>>> method)
        {
            MethodInfo memberInfo = null;
            if (method != null) memberInfo = Utilities.GetMethodMember(method);
            _CanExecuteMethod = memberInfo;
            return this;
        }
        public CommandConfiguration<T> CanExecute(Expression<Func<Func<bool>>> method)
        {
            var exp = Expression.Lambda<Func<T, Func<bool>>>(method.Body, Expression.Parameter(typeof(T), "m"));
            return CanExecute(exp);
        }
        public CommandConfiguration<T> CanExecute<TParameter>(Expression<Func<T, Func<TParameter, bool>>> method)
        {
            MethodInfo memberInfo = null;
            if (method != null) memberInfo = Utilities.GetMethodMember(method);
            _CanExecuteMethod = memberInfo;
            return this;
        }
        public CommandConfiguration<T> CanExecute<TParameter>(Expression<Func<Func<TParameter, bool>>> method)
        {
            var exp = Expression.Lambda<Func<T, Func<TParameter, bool>>>(method.Body, Expression.Parameter(typeof(T), "m"));
            return CanExecute(exp);
        }
        public override string ToString()
        {
            var name = (_Name ?? _Method.Name);
            return (name!=null? name.Append("()") : base.ToString());
        }
        #region ICommandConfiguration
        MethodInfo ICommandConfiguration.Method
        {
            get { return _Method; }
        }
        string ICommandConfiguration.Name
        {
            get { return _Name; }
        }
        MethodInfo ICommandConfiguration.CanExecuteMethod
        {
            get { return _CanExecuteMethod; }
        }
        Type ICommandConfiguration.FirstParameterType
        {
            get { return _FirstParameterType; }
        }
        #endregion
    }
    public class PropertyConfiguration<T, TProperty> : IPropertyConfiguration
    {
        private readonly PropertyInfo _BaseProperty;
        private readonly MethodInfo _GetterMethod;
        private object _DefaultValue;
        private MethodInfo _ChangingMethod;
        private MethodInfo _ChangedMethod;
        private readonly HashSet<string> _DependencyProperties = new HashSet<string>(StringComparer.Ordinal);
        private bool _HasDefaultValue = false;

        public PropertyConfiguration(PropertyInfo baseProperty)
        {
            if (baseProperty != null)
            {
                _BaseProperty = baseProperty;
                _GetterMethod = baseProperty.GetGetMethod(true);
            }
            _DefaultValue = default(TProperty);
        }
        public PropertyConfiguration<T, TProperty> HasDefaultValue(TProperty value)
        {
            if (!object.Equals(value, default(TProperty)))
            {
                if (typeof(TProperty) == typeof(Type))
                    throw new NotSupportedException($"A property with type '{typeof(Type).FullName}' can not has default value. please set it in 'Initialize' method.");

                _DefaultValue = value;
                _HasDefaultValue = true;
            }
            else
            {
                _HasDefaultValue = false;
            }
            return this;
        }
        public PropertyConfiguration<T, TProperty> HasNewInstance()
        {
            return HasNewInstance(typeof(TProperty));
        }
        public PropertyConfiguration<T, TProperty> HasNewInstance(Type type)
        {
            _DefaultValue = type;
            _HasDefaultValue = true;
            return this;
        }
        public PropertyConfiguration<T, TProperty> OnChanging(Expression<Func<T, Action>> method)
        {
            var memberInfo = Utilities.GetMethodMember(method);
            _ChangingMethod = memberInfo;
            return this;
        }
        public PropertyConfiguration<T, TProperty> OnChanging(Expression<Func<Action>> method)
        {
            var exp = Expression.Lambda<Func<T, Action>>(method.Body, Expression.Parameter(typeof(T), "m"));
            return OnChanging(exp);
        }
        public PropertyConfiguration<T, TProperty> OnChanging(string methodName)
        {
            var memberInfo = typeof(T).GetMethod(methodName, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public);
            _ChangingMethod = memberInfo;
            return this;
        }
        public PropertyConfiguration<T, TProperty> OnChanged(Expression<Func<T, Action>> method)
        {
            var memberInfo = Utilities.GetMethodMember(method);
            _ChangedMethod = memberInfo;
            return this;
        }
        public PropertyConfiguration<T, TProperty> OnChanged(Expression<Func<Action>> method)
        {
            var exp = Expression.Lambda<Func<T, Action>>(method.Body, Expression.Parameter(typeof(T), "m"));
            return OnChanged(exp);
        }
        public PropertyConfiguration<T, TProperty> OnChanged(string methodName)
        {
            var memberInfo = typeof(T).GetMethod(methodName, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public);
            _ChangedMethod = memberInfo;
            return this;
        }
        public PropertyConfiguration<T, TProperty> HasDependency<TDependencyProperty>(Expression<Func<T, TDependencyProperty>> property)
        {
            TypeHelper.GetMembers(property).OfType<PropertyInfo>().ForEach(m => _DependencyProperties.Add(m.Name));
            return this;
        }
        public PropertyConfiguration<T, TProperty> HasDependency<TDependencyProperty>(Expression<Func<TDependencyProperty>> property)
        {
            var exp = Expression.Lambda<Func<T, TDependencyProperty>>(property.Body, Expression.Parameter(typeof(T), "m"));
            return HasDependency(exp);
        }
        public PropertyConfiguration<T, TProperty> HasDependency(string propertyName)
        {
            _DependencyProperties.Add(propertyName);
            return this;
        }
        public override string ToString()
        {
            return (_BaseProperty?.Name ?? base.ToString());
        }
        #region IViewModelPropertyConfiguration
        PropertyInfo IPropertyConfiguration.BaseProperty
        {
            get { return _BaseProperty; }
        }
        object IPropertyConfiguration.DefaultValue
        {
            get { return _DefaultValue; }
        }
        bool IPropertyConfiguration.HasDefaultValue
        {
            get { return _HasDefaultValue; }
        }
        MethodInfo IPropertyConfiguration.ChangingMethod
        {
            get { return _ChangingMethod; }
        }
        MethodInfo IPropertyConfiguration.ChangedMethod
        {
            get { return _ChangedMethod; }
        }
        ICollection<string> IPropertyConfiguration.DependencyProperties
        {
            get { return _DependencyProperties; }
        }
        #endregion
    }
    public class ViewModelConfiguration<T> : IViewModelConfiguration
    {
        private readonly Type _Type = typeof(T);
        private readonly Dictionary<string, IPropertyConfiguration> _Properties = new Dictionary<string, IPropertyConfiguration>(StringComparer.Ordinal);
        private readonly Dictionary<string, ICommandConfiguration> _Commands = new Dictionary<string, ICommandConfiguration>(StringComparer.Ordinal);
        private MethodInfo _OnCreatedMethod;

        public PropertyConfiguration<T, TProperty> Property<TProperty>(Expression<Func<T, TProperty>> property)
        {
            var properties = TypeHelper.GetMembers(property).OfType<PropertyInfo>();
            AddProperties(properties);

            var propCount = properties.Count();
            if (propCount != 1)
                return null;

            return (PropertyConfiguration<T, TProperty>)_Properties[properties.First().Name];
        }
        public PropertyConfiguration<T, TProperty> Property<TProperty>(Expression<Func<TProperty>> property)
        {
            var exp = Expression.Lambda<Func<T, TProperty>>(property.Body, Expression.Parameter(typeof(T), "m"));
            return Property(exp);
        }
        public ViewModelConfiguration<T> Property(params PropertyInfo[] properties)
        {
            AddProperties(properties);
            return this;
        }
        public ViewModelConfiguration<T> Property(params string[] propertyNames)
        {
            AddProperties(propertyNames.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => _Type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)));
            return this;
        }
        private IPropertyConfiguration Property(PropertyInfo property)
        {
            AddProperties(new[] { property });

            return _Properties[property.Name];
        }
        public ViewModelConfiguration<T> OnInitilize(Expression<Func<T, Action>> method)
        {
            var memberInfo = Utilities.GetMethodMember(method);
            _OnCreatedMethod = memberInfo;
            return this;
        }
        public ViewModelConfiguration<T> OnInitilize(Expression<Func<Action>> method)
        {
            var exp = Expression.Lambda<Func<T, Action>>(method.Body, Expression.Parameter(typeof(T), "m"));
            return OnInitilize(exp);
        }
        public ViewModelConfiguration<T> OnInitilize(string methodName)
        {
            var memberInfo = _Type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public);
            _OnCreatedMethod = memberInfo;
            return this;
        }
        private ViewModelConfiguration<T> OnInitilize(MethodInfo method)
        {
            _OnCreatedMethod = method;
            return this;
        }
        private CommandConfiguration<T> Command(MethodInfo method)
        {
            return AddCommand(method);
        }
        public CommandConfiguration<T> Command(string methodName)
        {
            var memberInfo = _Type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public);
            return AddCommand(memberInfo);
        }
        public CommandConfiguration<T> Command(Expression<Func<T, Action>> method)
        {
            var memberInfo = Utilities.GetMethodMember(method);
            return AddCommand(memberInfo);
        }
        public CommandConfiguration<T> Command(Expression<Func<Action>> method)
        {
            var exp = Expression.Lambda<Func<T, Action>>(method.Body, Expression.Parameter(typeof(T), "m"));
            return Command(exp);
        }
        public CommandConfiguration<T> Command<TParameter>(Expression<Func<T, Action<TParameter>>> method)
        {
            var memberInfo = Utilities.GetMethodMember(method);
            return AddCommand(memberInfo);
        }
        public CommandConfiguration<T> Command<TParameter>(Expression<Func<Action<TParameter>>> method)
        {
            var exp = Expression.Lambda<Func<T, Action<TParameter>>>(method.Body, Expression.Parameter(typeof(T), "m"));
            return Command(exp);
        }
        public CommandConfiguration<T> Command<TResult>(Expression<Func<T, Func<TResult>>> method)
        {
            var memberInfo = Utilities.GetMethodMember(method);
            return AddCommand(memberInfo);
        }
        public CommandConfiguration<T> Command<TResult>(Expression<Func<Func<TResult>>> method)
        {
            var exp = Expression.Lambda<Func<T, Func<TResult>>>(method.Body, Expression.Parameter(typeof(T), "m"));
            return Command(exp);
        }
        public CommandConfiguration<T> Command<TParameter, TResult>(Expression<Func<T, Func<TParameter, TResult>>> method)
        {
            var memberInfo = Utilities.GetMethodMember(method);
            return AddCommand(memberInfo);
        }
        public CommandConfiguration<T> Command<TParameter, TResult>(Expression<Func<Func<TParameter, TResult>>> method)
        {
            var exp = Expression.Lambda<Func<T, Func<TParameter, TResult>>>(method.Body, Expression.Parameter(typeof(T), "m"));
            return Command(exp);
        }
        private void AddProperties(IEnumerable<PropertyInfo> properties)
        {
            if (properties == null)
                return;

            foreach (var memberInfo in properties)
            {
                if (_Properties.ContainsKey(memberInfo.Name)) continue;
                var configType = typeof(PropertyConfiguration<,>).MakeGenericType(_Type, memberInfo.PropertyType);
                var config = (IPropertyConfiguration)Activator.CreateInstance(configType, new object[] { memberInfo });
                _Properties.Add(memberInfo.Name, config);
            }
        }
        private CommandConfiguration<T> AddCommand(MethodInfo method)
        {
            ICommandConfiguration config = null;
            if (!_Commands.TryGetValue(method.Name, out config))
            {
                config = new CommandConfiguration<T>(method);
                _Commands[method.Name] = config;
            }
            return (CommandConfiguration<T>)config;
        }
        #region IViewModelConfiguration
        Type IViewModelConfiguration.Type
        {
            get { return _Type; }
        }
        MethodInfo IViewModelConfiguration.OnCreatedMethod
        {
            get { return _OnCreatedMethod; }
        }
        ICollection<IPropertyConfiguration> IViewModelConfiguration.Properties
        {
            get { return _Properties.Values; }
        }
        ICollection<ICommandConfiguration> IViewModelConfiguration.Commands
        {
            get { return _Commands.Values; }
        }
        #endregion
    }
}
namespace Golden.Mvvm.Configuration.Annotations
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Property : Attribute
    {
        public object DefaultValue { get; set; }
        public string OnChanging { get; set; }
        public string OnChanged { get; set; }
        public bool HasNewInstance { get; set; }
        public Type HasNewInstanceType { get; set; }
        public string[] Dependencies { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class Command : Attribute
    {
        public string Name { get; set; }
        public string CanExecute { get; set; }

        public Command() : this(null)
        {
        }
        public Command(string name)
        {
            this.Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class Initialize : Attribute
    {
    }
}
