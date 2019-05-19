using System;
using System.Windows.Input;

namespace PocketX.Handlers
{
    internal class SimpleCommand : ICommand
    {
        public SimpleCommand(Action<object> todo, Func<object, bool> check = null)
        {
            _todoAction = todo;
            _checkAction = check ?? (obj => true);
        }

        public event EventHandler CanExecuteChanged;
        private readonly Action<object> _todoAction;
        private readonly Func<object, bool> _checkAction;

        public bool CanExecute(object parameter) => _checkAction(parameter);

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
                _todoAction(parameter);
        }
    }
}