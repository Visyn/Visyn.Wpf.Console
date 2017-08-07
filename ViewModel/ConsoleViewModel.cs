using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using Visyn.Core.Collection;
using Visyn.Core.IO;
using Visyn.Public.Io;

namespace Visyn.Wpf.Console.ViewModel
{
    public class ConsoleViewModel : INotifyPropertyChanged, IOutputDeviceMultiline
    {
        public int MaxCount { get; set; }

        protected readonly IOutputDevice Output;
        private ICommand _executeItemCommand;
        private readonly ObservableCollectionExtended<string> _items;
        protected Dispatcher UiDispatcher { get; }

        //public ConsoleViewModel(int maxSize = 10000) : this(Dispatcher.CurrentDispatcher)
        //{
        //    MaxCount = maxSize;
        //}

        public ConsoleViewModel(int maxSize = 10000, Dispatcher dispatcher=null)
        {
            MaxCount = maxSize;
            UiDispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
            _items = new ObservableCollectionExtended<string>();
            Output = new BackgroundOutputDevice(Dispatcher.CurrentDispatcher, new OutputToCollection(_items), null);

            _executeItemCommand = new RelayCommand<string>(Add, x => true);
        }

        public ObservableCollection<string> Items => _items;

        public ICommand ExecuteItemCommand
        {
            get { return _executeItemCommand; }

            set { SetPropertyAndNotify(ref _executeItemCommand, value, nameof(ExecuteItemCommand)); }
        }


        public void Clear()
        {
            _items.Clear();
        }

        #region  IOutputDevice

        // TODO: Should implement partial line add here...
        public void Write(string text)
        {
            Add(text);
            //if (UiDispatcher.CheckAccess()) _items.Add(text);
            //else UiDispatcher.Invoke(new Action(() => _items.Add(text)));
        }

        public void WriteLine(string line)
        {
            Add(line);
            //if (UiDispatcher.CheckAccess())
            //{
            //    _items.Add(line);
            //    if (_items.Count > 100)
            //    {
            //        var toRemove = _items.FirstItems(10);
            //        _items.RemoveItems(toRemove);
            //    }
            //}
            //else UiDispatcher.Invoke(new Action(() => _items.Add(line)));
        }

        public void Write(Func<string> func)
        {
            Write(func());
        }

        public void Write(IEnumerable<string> lines)
        {
            Add(lines);
            //if (UiDispatcher.CheckAccess()) _items.AddRange(lines);
            //else UiDispatcher.Invoke(new Action(() => _items.AddRange(lines)));
        }

        protected void Add(string line)
        {
            if (!UiDispatcher.CheckAccess())
            {
                UiDispatcher.Invoke(() => Add(line));
                return;
            }
            _items.Add(line);
        }

        protected void Add(IEnumerable<string> lines)
        {
            if (!UiDispatcher.CheckAccess())
            {
                UiDispatcher.Invoke(() => Add(lines));
                return;
            }
            _items.AddRange(lines);
            if (_items.Count <= MaxCount) return;
            var toRemove = _items.FirstItems(MaxCount/10);
            _items.RemoveItems(toRemove);
        }



        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetPropertyAndNotify<T>(ref T existingValue, T newValue, string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(existingValue, newValue)) return false;

            existingValue = newValue;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            return true;
        }
    }
}
