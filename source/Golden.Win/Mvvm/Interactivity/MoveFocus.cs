using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Golden.Mvvm.Interactivity
{
    public sealed class MoveFocus : TriggerAction<Control>
    {
        public static readonly DependencyProperty MoveNextProperty = DependencyProperty.Register(
            nameof(MoveNext),
            typeof(bool),
            typeof(MoveFocus),
            new PropertyMetadata(false));
        public bool MoveNext
        {
            get { return (bool)GetValue(MoveNextProperty); }
            set { SetValue(MoveNextProperty, value); }
        }

        public static readonly DependencyProperty MoveFirstProperty = DependencyProperty.Register(
            nameof(MoveFirst),
            typeof(bool),
            typeof(MoveFocus),
            new PropertyMetadata(false));
        public bool MoveFirst
        {
            get { return (bool)GetValue(MoveFirstProperty); }
            set { SetValue(MoveFirstProperty, value); }
        }

        protected override void Invoke(object parameter)
        {
            if (base.AssociatedObject != null)
            {
                Action fnFocus = null;
                if (this.MoveNext)
                {
                    fnFocus = () => base.AssociatedObject.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
                else if (this.MoveFirst)
                {
                    fnFocus = () => base.AssociatedObject.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                }
                else
                {
                    Control focusCtrl = (base.AssociatedObject.Focusable && base.AssociatedObject.IsTabStop ? base.AssociatedObject : null);
                    if (focusCtrl == null)
                        focusCtrl = Golden.Win.Utility.WPFUtilities.GetVisualTreeChildren(base.AssociatedObject).OfType<Control>().FirstOrDefault(c => c.Focusable && c.IsTabStop);
                    fnFocus = () => focusCtrl?.Focus();
                }
#if NET45
                base.Dispatcher.InvokeAsync(fnFocus);
#else
                base.Dispatcher.BeginInvoke(fnFocus);
#endif
            }
        }
    }
}
