﻿using System;
using System.Windows.Input;

namespace IPAAnalyzer.UI
{
    public class DelegateCommand : ICommand  
    {
        public delegate void SimpleEventHandler();
        private SimpleEventHandler handler;
        private bool isEnabled = true;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(SimpleEventHandler handler)
        {
            this.handler = handler;
        }

        private void OnCanExecuteChanged()
        {
            if (this.CanExecuteChanged != null) {
                this.CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        bool ICommand.CanExecute(object arg)
        {
            return this.IsEnabled;
        }

        void ICommand.Execute(object arg)
        {
            this.handler();
        }

        public bool IsEnabled
        {
            get { return this.isEnabled; }

            set
            {
                this.isEnabled = value;
                this.OnCanExecuteChanged();
            }
        }
    }
}
