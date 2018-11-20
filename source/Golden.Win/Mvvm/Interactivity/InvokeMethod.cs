using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Golden.Mvvm.Interactivity
{
    public sealed class InvokeMethod : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty PassEventArgsToMethodProperty = DependencyProperty.Register(
            nameof(PassEventArgsToMethod),
            typeof(bool),
            typeof(InvokeMethod),
            new PropertyMetadata(false));
        public bool PassEventArgsToMethod
        {
            get { return (bool)GetValue(PassEventArgsToMethodProperty); }
            set { SetValue(PassEventArgsToMethodProperty, value); }
        }

        public static readonly DependencyProperty MarkRoutedEventsAsHandledProperty = DependencyProperty.Register(
            nameof(MarkRoutedEventsAsHandled),
            typeof(bool),
            typeof(InvokeMethod),
            new PropertyMetadata(false));
        public bool MarkRoutedEventsAsHandled
        {
            get { return (bool)GetValue(MarkRoutedEventsAsHandledProperty); }
            set { SetValue(MarkRoutedEventsAsHandledProperty, value); }
        }

        public static readonly DependencyProperty MethodNameProperty = DependencyProperty.Register(
            nameof(MethodName),
            typeof(string),
            typeof(InvokeMethod));
        public string MethodName
        {
            get { return (string)GetValue(MethodNameProperty); }
            set { SetValue(MethodNameProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(object),
            typeof(InvokeMethod));
        public object Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty MethodParameterProperty = DependencyProperty.Register(
            nameof(MethodParameter), 
            typeof(object), 
            typeof(InvokeMethod));
        public object MethodParameter
        {
            get { return GetValue(MethodParameterProperty); }
            set { SetValue(MethodParameterProperty, value); }
        }

        public static readonly DependencyProperty EventArgsConverterProperty = DependencyProperty.Register(
            nameof(EventArgsConverter),
            typeof(IEventArgsConverter),
            typeof(InvokeMethod));
        public IEventArgsConverter EventArgsConverter
        {
            get { return (IEventArgsConverter)GetValue(EventArgsConverterProperty); }
            set { SetValue(EventArgsConverterProperty, value); }
        }

        public static readonly DependencyProperty EventArgsConverterParameterProperty = DependencyProperty.Register(
            nameof(EventArgsConverterParameter),
            typeof(object),
            typeof(InvokeMethod));
        public object EventArgsConverterParameter
        {
            get { return GetValue(EventArgsConverterParameterProperty); }
            set { SetValue(EventArgsConverterParameterProperty, value); }
        }

        protected override void Invoke(object parameter)
        {
            if (base.AssociatedObject != null)
            {
                var source = (this.Source ?? base.AssociatedObject);
                var method = this.ResolveMethod();
                var mParams = method.GetParameters();
                var paramValues = new List<object>();
                if (mParams.Length > 0)
                {
                    var paramValue = this.MethodParameter;
                    if (this.PassEventArgsToMethod)
                    {
                        paramValue = parameter;
                        if (this.EventArgsConverter != null)
                            paramValue = this.EventArgsConverter.Convert(paramValue, this.EventArgsConverterParameter);
                    }
                    paramValues.Add(paramValue);
                    if (mParams.Length == 2 && mParams[0].ParameterType == typeof(object))
                    {
                        paramValues.Insert(0, base.AssociatedObject);
                    }
                }
                method.Invoke((method.IsStatic ? null : source), paramValues.ToArray());
                if (this.MarkRoutedEventsAsHandled)
                {
                    var routedEArgs = parameter as RoutedEventArgs;
                    if (routedEArgs != null)
                        routedEArgs.Handled = true;
                }
            }
        }
        private MethodInfo ResolveMethod()
        {
            return (this.Source ?? base.AssociatedObject).GetType().GetMethod(this.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }
    }
}
