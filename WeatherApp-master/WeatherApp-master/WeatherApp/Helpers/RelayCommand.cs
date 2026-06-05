using System;
using System.Windows.Input;

namespace WeatherApp.Infrastructure.Commands
{
    /// <summary>
    /// Моя реализация команды, чтобы не писать каждый раз одно и то же
    /// </summary>
    public class LambdaCommand : ICommand
    {
        // Тут хранится метод, который будет выполняться
        private readonly Action<object> _onExecute;
        // Тут проверка, можно ли выполнить команду
        private readonly Predicate<object> _onCanExecute;
        // Для команд без параметров сделала отдельные поля, так удобнее
        private readonly Action _onExecuteSimple;
        private readonly Func<bool> _onCanExecuteSimple;

        // Конструктор для команд, которым нужен параметр (например, id города)
        public LambdaCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _onExecute = execute ?? throw new ArgumentNullException(nameof(execute));
            _onCanExecute = canExecute;
        }

        // Конструктор для простых команд (без параметров), типа "обновить всё"
        public LambdaCommand(Action execute, Func<bool> canExecute = null)
        {
            _onExecuteSimple = execute ?? throw new ArgumentNullException(nameof(execute));
            _onCanExecuteSimple = canExecute;
        }

        // Проверяет, активна ли кнопка
        public bool CanExecute(object parameter)
        {
            // С параметрами
            if (_onCanExecute != null)
                return _onCanExecute(parameter);
            
            // Без параметров
            if (_onCanExecuteSimple != null)
                return _onCanExecuteSimple();
            
            return true;
        }

        // Выполняет команду
        public void Execute(object parameter)
        {
            if (_onExecute != null)
                _onExecute(parameter);
            else
                _onExecuteSimple?.Invoke();
        }

        // Метод для принудительного обновления состояния кнопки
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        // Событие, которое говорит UI, что состояние кнопки изменилось
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    /// <summary>
    /// Дженерик версия для команд с определенным типом параметра, чтобы не возиться с object
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            // Проверяем, что параметр нужного типа
            if (parameter is T typedParameter)
                return _canExecute == null || _canExecute(typedParameter);
            
            // Если параметр null и тип - ссылочный, тоже разрешаем (бывает нужно)
            if (parameter == null && !typeof(T).IsValueType)
                return _canExecute == null || _canExecute(default);
            
            return false;
        }

        public void Execute(object parameter)
        {
            if (parameter is T typedParameter)
                _execute(typedParameter);
            else if (parameter == null && !typeof(T).IsValueType)
                _execute(default);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Просто удобный метод для обновления
        public void Refresh()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
