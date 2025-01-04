using System;
using System.Windows.Input;

namespace XeryonMotionGUI.Helpers
{
    public class RelayCommand : ICommand
    {
        private readonly Action _executeNoParam;
        private readonly Action<object> _executeWithParam;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action execute, Func<object, bool> canExecute = null)
        {
            _executeNoParam = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _executeWithParam = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            if (_executeNoParam != null)
                _executeNoParam();
            else if (_executeWithParam != null)
                _executeWithParam(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
