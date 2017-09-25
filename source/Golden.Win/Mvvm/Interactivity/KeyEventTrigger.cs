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
    public enum KeyAction
    {
        KeyDown,
        KeyUp,
        PreviewKeyDown,
        PreviewKeyUp
    }

    public class KeyEventTrigger : EventTriggerBase<object>
    {
        private string eventName = "KeyDown";
        private KeyAction action = KeyAction.KeyDown;

        public KeyAction Action
        {
            get { return this.action; }
            set
            {
                if (this.action == value) return;
                this.action = value;
                ChangeEvent();
            }
        }
        
        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
            nameof(Key),
            typeof(Key),
            typeof(KeyEventTrigger),
            new FrameworkPropertyMetadata(Key.None));
        public Key Key
        {
            get{return (Key)base.GetValue(KeyProperty);}
            set{base.SetValue(KeyProperty, value);}
        }

        public static readonly DependencyProperty ModifiersProperty = DependencyProperty.Register(
            nameof(Modifiers),
            typeof(ModifierKeys),
            typeof(KeyEventTrigger),
            new FrameworkPropertyMetadata(ModifierKeys.None));
        public ModifierKeys Modifiers
        {
            get { return (ModifierKeys)base.GetValue(ModifiersProperty); }
            set { base.SetValue(ModifiersProperty, value); }
        }

        public KeyEventTrigger()
        {
        }
        protected override string GetEventName()
        {
            return this.eventName;
        }
        private void ChangeEvent()
        {
            var oldName = this.eventName;
            this.eventName = Enum.GetName(typeof(KeyAction), this.action);
            OnEventNameChanged(this, oldName, this.eventName);
        }
        private void OnEventNameChanged(object sender, string oldName, string newName)
        {
            var mOnChanged = typeof(KeyEventTrigger).GetMethod(
                nameof(OnEventNameChanged), 
                BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public);
            mOnChanged?.Invoke(sender, new object[] { oldName, newName });
        }
        protected override void OnEvent(EventArgs eventArgs)
        {
            base.OnEvent(eventArgs);
        }
    }
}
