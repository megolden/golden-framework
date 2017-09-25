using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Golden.Mvvm.Interactivity
{
    public enum MouseAction
    {
        MouseDown,
        MouseEnter,
        MouseLeave,
        MouseLeftButtonDown,
        MouseLeftButtonUp,
        MouseMove,
        MouseRightButtonDown,
        MouseRightButtonUp,
        MouseUp,
        MouseWheel
    }

    public class MouseEventTrigger : EventTriggerBase<object>
    {
        private string eventName = "MouseDown";
        private MouseAction action = MouseAction.MouseDown;

        public MouseAction Action
        {
            get { return this.action; }
            set
            {
                if (this.action == value) return;
                this.action = value;
                ChangeEvent();
            }
        }

        public static readonly DependencyProperty ModifiersProperty = DependencyProperty.Register(
            nameof(Modifiers),
            typeof(ModifierKeys),
            typeof(KeyEventTrigger),
            new FrameworkPropertyMetadata(ModifierKeys.None));
        public ModifierKeys Modifiers
        {
            get { return (ModifierKeys)GetValue(ModifiersProperty); }
            set { SetValue(ModifiersProperty, value); }
        }

        protected override string GetEventName()
        {
            return this.eventName;
        }
        private void ChangeEvent()
        {
            var oldName = this.eventName;
            this.eventName = Enum.GetName(typeof(MouseAction), this.action);
            if (base.AssociatedObject!=null) OnEventNameChanged(this, oldName, this.eventName);
        }
        private void OnEventNameChanged(object sender, string oldName, string newName)
        {
            var mOnChanged = typeof(MouseEventTrigger).GetMethod(
                nameof(OnEventNameChanged),
                BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public);
            mOnChanged?.Invoke(sender, new object[] { oldName, newName });
        }
        protected override void OnEvent(EventArgs eventArgs)
        {
            base.OnEvent(eventArgs);
            base.InvokeActions(eventArgs);
        }
    }
}
