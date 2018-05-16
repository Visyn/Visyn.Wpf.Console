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
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using Visyn.Collection;
using Visyn.Io;
using Visyn.Log;

namespace Visyn.Wpf.Console.ViewModel
{
    public class ConsoleWithSeverityViewModel : ConsoleViewModel, IOutputDevice<SeverityLevel>
    {
        protected readonly BackgroundOutputDeviceWithSeverity OutputSeverity;

        public ConsoleWithSeverityViewModel(int maxSize = 10000, Dispatcher dispatcher = null)
            : base(maxSize, dispatcher, CreateOutputDevice())
        {
            OutputSeverity = Output as BackgroundOutputDeviceWithSeverity;
        }

        public new static Func<ObservableCollectionExtended<object>, BackgroundOutputDeviceMultiline> CreateOutputDevice()
        {
            return ((collection) =>
            {
                var addRangeAction = new Action<IEnumerable<object>>((i) => collection.AddRange(i));
                var outputDevice = new BackgroundOutputDeviceWithSeverity(Dispatcher.CurrentDispatcher,
                    new OutputToCollectionSeverity(collection, addRangeAction), null);

                outputDevice.TaskStartedAction = (d) =>
                {
                    Thread.CurrentThread.Name = $"{outputDevice.Name} {Thread.CurrentThread.ManagedThreadId}";
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                };
                return outputDevice;
            });
        }

        #region Implementation of IOutputDevice<SeverityLevel>

        public void Write(string text, SeverityLevel type) => Add(new MessageWithSeverityLevel(text, type));

        public void WriteLine(string line, SeverityLevel type) => Add(new MessageWithSeverityLevel(line, type));

        public void Write(Func<string> func, SeverityLevel type) => Write(func(), type);

        #endregion
        protected void Add(object line)
        {
            var text = line as string;
            if (text != null)
            {
                OutputSeverity.WriteLine(text);
                return;
            }
            var severity = line as MessageWithSeverityLevel;
            if (severity != null)
            {
                OutputSeverity.Write(severity.Message, severity.SeverityLevel);
                return;
            }
            OutputSeverity.WriteLine(line.ToString());
        }

        #region Implementation of IExceptionHandler

        public override bool HandleException(object sender, Exception exception)
        {
            Write($"{sender?.GetType().Name} {exception.GetType().Name}: {exception.Message}", SeverityLevel.Error);
            return true;
        }

        #endregion

        #region Overrides of ConsoleViewModel

        public override void Dispose()
        {
            OutputSeverity.Dispose();
            base.Dispose();
        }

        #endregion
    }
}