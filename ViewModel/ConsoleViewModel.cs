#region Copyright (c) 2015-2017 Visyn
// The MIT License(MIT)
// 
// Copyright(c) 2015-2017 Visyn
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
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Visyn.Collection;
using Visyn.Exceptions;
using Visyn.Io;
using Visyn.Log;

namespace Visyn.Wpf.Console.ViewModel
{
    public class ConsoleViewModel : INotifyPropertyChanged, IOutputDevice<SeverityLevel>, IExceptionHandler//, IOutputDeviceMultiline
    {
        public int MaxCount { get; set; }

        protected readonly IOutputDevice Output;
        private ICommand _executeItemCommand;
        private readonly ObservableCollectionExtended<object> _items;
        protected Dispatcher UiDispatcher { get; }


        public ConsoleViewModel(int maxSize = 10000, Dispatcher dispatcher=null)
        {
            MaxCount = maxSize;
            UiDispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
            _items = new ObservableCollectionExtended<object>();
            
            Output = new BackgroundOutputDeviceMultiline(Dispatcher.CurrentDispatcher, new OutputToCollection<object>(_items, _items.AddRange),
                (t) => t + $"\t(queued {((BackgroundOutputDevice)Output)?.Count})");

            _executeItemCommand = new RelayCommand<string>(Add, x => true);
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

        public void WriteLine(string line)
        {
            Add(line);
        }

        public void Write(Func<string> func)
        {
            Write(func());
        }

        public void Write(IEnumerable<string> lines)
        {
            AddLines(lines);
        }

        #region Implementation of IOutputDevice<SeverityLevel>

        public void Write(string text, SeverityLevel type)
        {
            Add(new MessageWithSeverityLevel(text, type));
        }

        public void WriteLine(string line, SeverityLevel type)
        {
            Add(new MessageWithSeverityLevel(line, type));
        }

        public void Write(Func<string> func, SeverityLevel type)
        {
            Write(func(), type);
        }

        #endregion
        protected void Add(object line)
        {
            if (!UiDispatcher.CheckAccess())
            {
                var text = line as string;
                if (text != null) Output.WriteLine(text);
                else
                    UiDispatcher.Invoke(() => Add(line));
                //   UiDispatcher.Invoke(() => Add(line));
                return;
            }
            _items.Add(line);
        }

        protected void AddLines(IEnumerable lines)
        {
            if (!UiDispatcher.CheckAccess())
            {
                Output.Write(from object line in lines select line.ToString());
                //        UiDispatcher.Invoke(() => AddLines(lines));
                return;
            }
            _items.AddRange(lines);
            if (_items.Count <= MaxCount) return;
            var toRemove = _items.FirstItems(MaxCount / 10);
            _items.RemoveItems(toRemove);
        }



        #endregion
        #region Implementation of IExceptionHandler

        public bool HandleException(object sender, Exception exception)
        {
            WriteLine(exception.Message,SeverityLevel.Error);
            return true;
        }

        #endregion
    }
}
