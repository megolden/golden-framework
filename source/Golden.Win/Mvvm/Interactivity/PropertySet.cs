using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Interactivity;

namespace Golden.Mvvm.Interactivity
{
    public class PropertySet : TriggerAction<DependencyObject>
    {
        public bool CaseSensitivePropertyName { get; set; }

        public static readonly DependencyProperty PropertyNameProperty = DependencyProperty.Register(
            nameof(PropertyName),
            typeof(string),
            typeof(PropertySet));
        public string PropertyName
        {
            get { return (string)GetValue(PropertyNameProperty); }
            set { SetValue(PropertyNameProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(object),
            typeof(PropertySet));
        public object Value
        {
            get { return GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(object),
            typeof(PropertySet));
        public object Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public PropertySet()
        {
            this.CaseSensitivePropertyName = true;
        }
        protected override void Invoke(object parameter)
        {
            if (base.AssociatedObject != null)
            {
                var obj = (this.Source ?? base.AssociatedObject);
                var attrs = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
                if (!this.CaseSensitivePropertyName) attrs |= BindingFlags.IgnoreCase;
                var property = obj.GetType().GetProperty(this.PropertyName, attrs);
                var setter = property.GetSetMethod(true);
                setter?.Invoke((setter.IsStatic ? null : obj), new object[] { this.Value });
            }
        }
    }
}
