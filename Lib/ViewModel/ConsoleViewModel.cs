#region Copyright (c) 2015-2018 Visyn
// The MIT License(MIT)
// 
// Copyright (c) 2015-2018 Visyn
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using Visyn.Collection;
using Visyn.Exceptions;
using Visyn.Io;

namespace Visyn.Wpf.Console.ViewModel
{
    public class ConsoleViewModel : INotifyPropertyChanged, IExceptionHandler, IOutputDeviceMultiline, IDisposable
    {
        public int MaxCount { get; set; }

        private ICommand _executeItemCommand;
        public ICommand ClearCommand { get; set; }

        protected readonly ObservableCollectionExtended<object> _items;
    
        protected Dispatcher UiDispatcher { get; }
        protected readonly BackgroundOutputDeviceMultiline Output;

        protected ConsoleViewModel(int maxSize, Dispatcher dispatcher , Func<ObservableCollectionExtended<object>, BackgroundOutputDeviceMultiline> output)
        {
            MaxCount = maxSize;
            UiDispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
            _items = new ObservableCollectionExtended<object>();

            _executeItemCommand = new RelayCommand<string>(Add, x => true);
            ClearCommand = new RelayCommand<object>(Clear, x => true);
            Output = output.Invoke(_items);
        }

        public ConsoleViewModel(int maxSize = 10000, Dispatcher dispatcher=null) 
            : this(maxSize,dispatcher, CreateOutputDevice()) 
        {
        }

        public static Func<ObservableCollectionExtended<object>,BackgroundOutputDeviceMultiline> CreateOutputDevice()
        {
            return ((collection) =>
            {
                var outputDevice = new BackgroundOutputDeviceMultiline(Dispatcher.CurrentDispatcher,
                    new OutputToCollection<object>(collection, collection.AddRange), null);

                outputDevice.TaskStartedAction = (d) =>
                {
                    Thread.CurrentThread.Name = $"{outputDevice.Name} {Thread.CurrentThread.ManagedThreadId}";
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                };
                return outputDevice;
            });
        }

        public ObservableCollection<object> Items => _items;

        public ICommand ExecuteItemCommand
        {
            get { return _executeItemCommand; }

            set { SetPropertyAndNotify(ref _executeItemCommand, value, nameof(ExecuteItemCommand)); }
        }


        public void Clear()
        {
            _items.Clear();
        }
        protected void Clear(object param) => Clear();

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

        #region  IOutputDevice

        // TODO: Should implement partial line add here...
        public void Write(string text)
        {
            Add(text);
        }

        public void WriteLine(string line) => Add(line);

        public void Write(Func<string> func) => Write(func());

        public void Write(IEnumerable<string> lines) => AddLines(lines);


        protected void Add(string text)
        {
            if (text != null)
            {
                Output.WriteLine(text);
            }
        }

        protected void AddLines(IEnumerable lines)
        {
            foreach(var line in lines)
            {
                Output.Write(line?.ToString() ?? "");
            }
        }

        #endregion

        #region Implementation of IExceptionHandler

        public virtual bool HandleException(object sender, Exception exception)
        {
            WriteLine($"{sender?.GetType().Name} {exception.GetType().Name}: {exception.Message}");
            return true;
        }

        #endregion

        #region IDisposable

        public virtual void Dispose()
        {
            _items?.ClearWithoutNotify();
            Output?.Dispose();
        }

        #endregion
    }
}
