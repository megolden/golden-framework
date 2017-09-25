using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Golden.Mvvm.Interactivity
{
    public interface IEventArgsConverter
    {
        object Convert(object value, object args);
    }

    public sealed class InvokeCommand : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty PassEventArgsToCommandProperty = DependencyProperty.Register(
            nameof(PassEventArgsToCommand),
            typeof(bool),
            typeof(InvokeCommand),
            new PropertyMetadata(false));
        public bool PassEventArgsToCommand
        {
            get { return (bool)GetValue(PassEventArgsToCommandProperty); }
            set { SetValue(PassEventArgsToCommandProperty, value); }
        }

        public static readonly DependencyProperty MarkRoutedEventsAsHandledProperty = DependencyProperty.Register(
            nameof(MarkRoutedEventsAsHandled),
            typeof(bool),
            typeof(InvokeCommand),
            new PropertyMetadata(false));
        public bool MarkRoutedEventsAsHandled
        {
            get { return (bool)GetValue(MarkRoutedEventsAsHandledProperty); }
            set { SetValue(MarkRoutedEventsAsHandledProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(InvokeCommand));
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandNameProperty = DependencyProperty.Register(
            nameof(CommandName),
            typeof(string),
            typeof(InvokeCommand));
        public string CommandName
        {
            get { return (string)GetValue(CommandNameProperty); }
            set { SetValue(CommandNameProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(object),
            typeof(InvokeCommand));
        public object Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter), 
            typeof(object), 
            typeof(InvokeCommand));
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public static readonly DependencyProperty EventArgsConverterProperty = DependencyProperty.Register(
            nameof(EventArgsConverter),
            typeof(IEventArgsConverter),
            typeof(InvokeCommand));
        public IEventArgsConverter EventArgsConverter
        {
            get { return (IEventArgsConverter)GetValue(EventArgsConverterProperty); }
            set { SetValue(EventArgsConverterProperty, value); }
        }

        public static readonly DependencyProperty EventArgsConverterParameterProperty = DependencyProperty.Register(
            nameof(EventArgsConverterParameter),
            typeof(object),
            typeof(InvokeCommand));
        public object EventArgsConverterParameter
        {
            get { return GetValue(EventArgsConverterParameterProperty); }
            set { SetValue(EventArgsConverterParameterProperty, value); }
        }

        protected override void Invoke(object parameter)
        {
            if (base.AssociatedObject != null)
            {
                var command = this.ResolveCommand();
                if (command != null && command.CanExecute(this.CommandParameter))
                {
                    var paramValue = this.CommandParameter;
                    if (this.PassEventArgsToCommand)
                    {
                        paramValue = parameter;
                        if (this.EventArgsConverter != null)
                            paramValue = this.EventArgsConverter.Convert(paramValue, this.EventArgsConverterParameter);
                    }
                    command.Execute(paramValue);
                    if (this.MarkRoutedEventsAsHandled)
                    {
                        var routedEArgs = parameter as RoutedEventArgs;
                        if (routedEArgs != null)
                            routedEArgs.Handled = true;
                    }
                }
            }
        }
        private ICommand ResolveCommand()
        {
            if (this.Command != null)
                return this.Command;

            if (!string.IsNullOrEmpty(this.CommandName))
                return (ICommand)Golden.Utility.TypeHelper.GetMemberValue(this.CommandName, (this.Source ?? base.AssociatedObject));

            return null;
        }
    }
}
