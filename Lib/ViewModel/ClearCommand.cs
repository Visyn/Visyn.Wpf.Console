using System;

namespace Visyn.Wpf.Console.ViewModel
{
    /// <summary>
    /// Class ClearCommand : IRelayCommand{ConsoleViewModel}
    /// </summary>
    /// <seealso cref="Visyn.Wpf.Console.ViewModel.IRelayCommand{Visyn.Wpf.Console.ViewModel.ConsoleViewModel}" />
    public class ClearCommand : IRelayCommand<ConsoleViewModel>
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) =>  (parameter as ConsoleViewModel)?.Items.Count > 0;

        public void Execute(object parameter) => (parameter as ConsoleViewModel)?.Clear();
    }
}
