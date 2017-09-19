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
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using Visyn.JetBrains;

namespace Visyn.Wpf.Console
{
    public class WriteOnlyConsole : RichTextBox
    {
        private readonly Dispatcher _uiDispatcher;

        private readonly Paragraph _paragraph;
        private readonly Run _promptInline;

        /// <summary>
        /// The items to be displayed in the terminal window, e.g. an ObservableCollection.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(WriteOnlyConsole),
            new PropertyMetadata(default(IEnumerable), OnItemsSourceChanged));

        /// <summary>
        /// The height of each line in the terminal window, optional field with a default value of 10.
        /// </summary>
        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(nameof(ItemHeight),
            typeof(int),
            typeof(WriteOnlyConsole),
            new PropertyMetadata(10, OnItemHeightChanged));

        /// <summary>
        /// The margin around the contents of the terminal window, optional field with a default value of 0.
        /// </summary>
        public static readonly DependencyProperty ItemsMarginProperty = DependencyProperty.Register(nameof(ItemsMargin),
            typeof(Thickness),
            typeof(WriteOnlyConsole),
            new PropertyMetadata(new Thickness(), OnItemsMarginChanged));

        private static void OnItemsMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue == args.OldValue) return;

            ((WriteOnlyConsole)d)._paragraph.Margin = (Thickness)args.NewValue;
        }

        private static void OnItemHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue == args.OldValue) return;

            ((WriteOnlyConsole)d)._paragraph.LineHeight = (int)args.NewValue;
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue == args.OldValue) return;

            ((WriteOnlyConsole)d).HandleItemsSourceChanged((IEnumerable)args.NewValue);
        }

        private static void OnPromptChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue == args.OldValue) return;

            ((WriteOnlyConsole)d).HandlePromptChanged((string)args.NewValue);
        }
        /// <summary>
        /// The terminal prompt to be displayed.
        /// </summary>
        public static readonly DependencyProperty PromptProperty = DependencyProperty.Register(nameof(Prompt),
            typeof(string),
            typeof(WriteOnlyConsole),
            new PropertyMetadata(default(string), OnPromptChanged));


        /// <summary>
        /// The bound items to the terminal.
        /// </summary>
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        /// <summary>
        /// The prompt of the terminal.
        /// </summary>
        public string Prompt
        {
            get { return (string)GetValue(PromptProperty); }
            set { SetValue(PromptProperty, value); }
        }

        /// <summary>
        /// The individual line height for the bound items.
        /// </summary>
        public int ItemHeight
        {
            get { return (int)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        /// <summary>
        /// The margin around the bound items.
        /// </summary>
        public Thickness ItemsMargin
        {
            get { return (Thickness)GetValue(ItemsMarginProperty); }
            set { SetValue(ItemsMarginProperty, value); }
        }

        public WriteOnlyConsole() : this(Dispatcher.CurrentDispatcher)
        {
            
        }

        public WriteOnlyConsole(Dispatcher dispatcher)
        {
            _uiDispatcher = dispatcher;
            _paragraph = new Paragraph
            {
                Margin = ItemsMargin,
                LineHeight = ItemHeight
            };

            IsUndoEnabled = false;
            var prompt = !string.IsNullOrEmpty(Prompt) ? Prompt : ">";
            _promptInline = new Run(prompt);
            Document = new FlowDocument();//_paragraph);

            // Create a paragraph with text
            //Paragraph para = new Paragraph();
            _paragraph.Inlines.Add(new Run("I am a flow document. Would you like to edit me? "));
            _paragraph.Inlines.Add(new Bold(new Run("Go ahead.")));

            // Add the paragraph to blocks of paragraph
            Document.Blocks.Add(_paragraph);
            AddPrompt();
        }

        private void HandlePromptChanged(string prompt)
        {
            if (_promptInline == null) return;

            _promptInline.Text = prompt;
        }

        private void AddLine(string line)
        {
            CaretPosition = CaretPosition.DocumentEnd;

            _paragraph.Inlines.Add(new Run(line));

            CaretPosition = Document.ContentEnd;
        }

        private void AddItems(object[] items)
        {
            Debug.Assert(items != null);

            var command = AggregateAfterPrompt();
            ClearAfterPrompt();
            _paragraph.Inlines.Remove(_promptInline);

            var inlines = items.SelectMany(x =>
            {
                var value = ExtractValue(x);

                var newInlines = new List<Inline>();
                using (var reader = new StringReader(value))
                {
                    var line = reader.ReadLine();

                    newInlines.Add(new Run(line));   // newInlines.Add(new Run(line) { Foreground = GetForegroundColor(x) });
                    newInlines.Add(new LineBreak());
                }
                return newInlines;
            }).ToArray();

            _paragraph.Inlines.AddRange(inlines);
            AddPrompt();
            _paragraph.Inlines.Add(new Run(command));
            CaretPosition = CaretPosition.DocumentEnd;
        }


        private string AggregateAfterPrompt()
        {
            var inlineList = _paragraph.Inlines.ToList();
            var promptIndex = inlineList.IndexOf(_promptInline);

            return inlineList.Where((x, i) => i > promptIndex).Where(x => x is Run).Cast<Run>()
                .Select(x => x.Text).Aggregate(string.Empty, (current, part) => current + part);
        }

        private void ClearAfterPrompt()
        {
            var inlineList = _paragraph.Inlines.ToList();
            var promptIndex = inlineList.IndexOf(_promptInline);

            foreach (var inline in inlineList.Where((x, i) => i > promptIndex))
            {
                _paragraph.Inlines.Remove(inline);
            }
        }

        [NotNull]
        private string ExtractValue(object item)
        {
#if true
            return item?.ToString() ?? string.Empty;
#else
            var displayPath = ItemDisplayPath;
            if (displayPath == null)
            {
                return item?.ToString() ?? string.Empty;
            }

            if (_displayPathProperty == null)
            {
                _displayPathProperty = item.GetType().GetProperty(displayPath);
            }

            return _displayPathProperty.GetValue(item, null)?.ToString() ?? string.Empty;
#endif
        }

        //private Brush GetForegroundColor(object item)
        //{
        //    if (LineColorConverter != null)
        //    {
        //        return (Brush)LineColorConverter.Convert(item, typeof(Brush), null, CultureInfo.InvariantCulture);
        //    }
        //    return Foreground;
        //}

        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        private void RemoveItems(object[] items)
        {
            foreach (var item in items)
            {
                var value = ExtractValue(item);

                var run = _paragraph.Inlines.Where(x => x is Run).Cast<Run>().FirstOrDefault(x => x.Text == value);

                if (run != null)
                {
                    _paragraph.Inlines.Remove(run);
                }
            }
        }

        private void HandleItemsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            using (DeclareChangeBlock())
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        AddItems(args.NewItems.Cast<object>().ToArray());
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        RemoveItems(args.OldItems.Cast<object>().ToArray());
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        ReplaceItems(((IEnumerable)sender).Cast<object>().ToArray());
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        RemoveItems(args.OldItems.Cast<object>().ToArray());
                        AddItems(args.NewItems.Cast<object>().ToArray());
                        break;
                    case NotifyCollectionChangedAction.Move:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        private INotifyCollectionChanged _notifyChanged;
        private void HandleItemsSourceChanged(IEnumerable items)
        {
            if (items == null)
            {
                _paragraph.Inlines.Clear();
                AddPrompt();
                return;
            }

            using (DeclareChangeBlock())
            {
                var changed = items as INotifyCollectionChanged;
                if (changed != null)
                {
                    if (_notifyChanged != null)
                    {
                        _notifyChanged.CollectionChanged -= HandleItemsChanged;
                    }

                    _notifyChanged = changed;
                    _notifyChanged.CollectionChanged += HandleItemsChanged;

                    var existingItems = items.Cast<object>().ToArray();
                    if (existingItems.Any())
                    {
                        ReplaceItems(existingItems);
                    }
                    else
                    {
                        ClearItems();
                    }
                }
                else
                {
                    ReplaceItems(ItemsSource.Cast<object>().ToArray());
                }
            }
        }

        private void ClearItems()
        {
            _paragraph.Inlines.Clear();
            AddPrompt();
        }

        private void ReplaceItems(object[] items)
        {
            Debug.Assert(items != null);

            _paragraph.Inlines.Clear();
            AddItems(items);
        }

        private void AddPrompt()
        {
            _paragraph.Inlines.Add(_promptInline);
            _paragraph.Inlines.Add(new Run());

            CaretPosition = Document.ContentEnd;
        }

        //public void Write(string text)
        //{
        //    if (_uiDispatcher.CheckAccess())
        //    {
        //        AddLine(text);
        //    }
        //    else
        //    {
        //        _uiDispatcher.BeginInvoke(new Action(() => AddLine(text)));
        //    }
        //}

        //public void WriteLine(string line)
        //{
        //    if (_uiDispatcher.CheckAccess())
        //    {
        //        AddLine(line);
        //    }
        //    else
        //    {
        //        _uiDispatcher.BeginInvoke(new Action(() => AddLine(line)));
        //    }
        //}

        //public void Write(Func<string> func)
        //{
        //    if (_uiDispatcher.CheckAccess())
        //    {
        //        AddLine(func());
        //    }
        //    else
        //    {
        //        _uiDispatcher.BeginInvoke(new Action(() => AddLine(func())));
        //    }
        //}

        //public void Write(IEnumerable<string> lines)
        //{
        //    var text = lines.Aggregate(string.Empty, (current, line) => current + (line + Environment.NewLine));
        //    if (_uiDispatcher.CheckAccess())
        //    {
        //        AddLine(text);
        //    }
        //    else
        //    {
        //        _uiDispatcher.BeginInvoke(new Action(() => AddLine(text)));
        //    }
        //}
    }
}
