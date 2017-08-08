using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Visyn.Public.JetBrains;
using Visyn.Public.Types;

namespace Visyn.Wpf.Console
{
    /// <summary>
    /// A WPF user control which mimics a terminal\console window, you are responsible for the service
    /// providing the data for display and processing the entered line when the LineEntered event is raised.
    /// The data is bound via the ItemsSource dependency property.
    /// </summary>
    public partial class Terminal : RichTextBox, IConsole
    {
        /// <summary>
        /// Event fired when the user presses the Enter key.
        /// </summary>
        public event EventHandler LineEntered;

        /// <summary>
        /// The items to be displayed in the terminal window, e.g. an ObservableCollection.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(Terminal),
            new PropertyMetadata(default(IEnumerable), OnItemsSourceChanged));

        /// <summary>
        /// Autocompletion-strings to be traversed in terminal window when tab is pressed, e.g. an ObservableCollection.
        /// </summary>
        public static readonly DependencyProperty AutoCompletionsSourceProperty = DependencyProperty.Register(nameof(AutoCompletionsSource),
            typeof(IEnumerable<string>),
            typeof(Terminal),
            new PropertyMetadata(default(IEnumerable<string>)));

	    /// <summary>
        /// The margin around the contents of the terminal window, optional field with a default value of 0.
        /// </summary>
        public static readonly DependencyProperty ItemsMarginProperty = DependencyProperty.Register(nameof(ItemsMargin),
            typeof(Thickness),
            typeof(Terminal),
            new PropertyMetadata(new Thickness(), OnItemsMarginChanged));

        /// <summary>
        /// The terminal prompt to be displayed.
        /// </summary>
        public static readonly DependencyProperty PromptProperty = DependencyProperty.Register(nameof(Prompt),
            typeof(string),
            typeof(Terminal),
            new PropertyMetadata(default(string), OnPromptChanged));

        /// <summary>
        /// The current the editable line in the terminal, there is only one editable line in the terminal and this is at the bottom of the content.
        /// </summary>
        public static readonly DependencyProperty LineProperty = DependencyProperty.Register(nameof(Line),
            typeof(string),
            typeof(Terminal),
            new PropertyMetadata(default(string)));

        /// <summary>
        /// The property name of the 'value' to be displayed, optional field which if null then ToString() is called on the
        /// bound instance.
        /// </summary>
        public static readonly DependencyProperty ItemDisplayPathProperty = DependencyProperty.Register(nameof(ItemDisplayPath),
            typeof(string),
            typeof(Terminal),
            new PropertyMetadata(default(string), OnDisplayPathChanged));

        /// <summary>
        /// The color converter for lines.
        /// </summary>
        public static readonly DependencyProperty LineColorConverterProperty = DependencyProperty.Register(nameof(LineColorConverter),
            typeof(IValueConverter),
            typeof(Terminal),
            new PropertyMetadata(null, OnLineConverterChanged));

        /// <summary>
        /// The height of each line in the terminal window, optional field with a default value of 10.
        /// </summary>
        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(nameof(ItemHeight),
            typeof(int),
            typeof(Terminal),
            new PropertyMetadata(10, OnItemHeightChanged));

        private readonly Paragraph _paragraph;
        private readonly List<string> _buffer = new List<string>();
        private readonly Run _promptInline;

        private int _autoCompletionIndex;
        private List<string> _currentAutoCompletionList = new List<string>();

        private INotifyCollectionChanged _notifyChanged;
        private PropertyInfo _displayPathProperty;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Terminal()
        {
            _paragraph = new Paragraph
            {
                Margin = ItemsMargin,
                LineHeight = ItemHeight
            };
            
            IsUndoEnabled = false;

            _promptInline = new Run(Prompt);
            Document = new FlowDocument(_paragraph);
            
            AddPrompt();
            
            TextChanged += (s, e) =>
                           {
	                           Line = AggregateAfterPrompt();
	                           ScrollToEnd();
                           };

            DataObject.AddPastingHandler(this, PasteCommand);
            DataObject.AddCopyingHandler(this, CopyCommand);

            SetResourceReference(StyleProperty, "TerminalStyle");
        }
        
        /// <summary>
        /// The bound items to the terminal.
        /// </summary>
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        /// <summary>
        /// The bound autocompletion-strings to the terminal.
        /// </summary>
        public IEnumerable<string> AutoCompletionsSource
        {
            get { return (IEnumerable<string>)GetValue(AutoCompletionsSourceProperty); }
            set { SetValue(AutoCompletionsSourceProperty, value); }
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
        /// The current editable line of the terminal (bottom line).
        /// </summary>
        public string Line
        {
            get { return (string)GetValue(LineProperty); }
            set { SetValue(LineProperty, value); }
        }

        /// <summary>
        /// The display path for the bound items.
        /// </summary>
        public string ItemDisplayPath
        {
            get { return (string)GetValue(ItemDisplayPathProperty); }
            set { SetValue(ItemDisplayPathProperty, value); }
        }

        /// <summary>
        /// The error color for the bound items.
        /// </summary>
        public IValueConverter LineColorConverter
        {
            get { return (IValueConverter)GetValue(LineColorConverterProperty); }
            set { SetValue(LineColorConverterProperty, value); }
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

        /// <summary>
        /// Processes style changes for the terminal.
        /// </summary>
        /// <param name="oldStyle">The current style applied to the terminal.</param>
        /// <param name="newStyle">The new style to be applied to the terminal.</param>
        protected override void OnStyleChanged(Style oldStyle, Style newStyle)
        {
            base.OnStyleChanged(oldStyle, newStyle);

            if (ItemsSource == null) return;
            using (DeclareChangeBlock())
            {
                ReplaceItems(ItemsSource.Cast<object>().ToArray());
            }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue == args.OldValue) return;

            ((Terminal)d).HandleItemsSourceChanged((IEnumerable)args.NewValue);
        }

        private static void OnPromptChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue == args.OldValue) return;

            ((Terminal)d).HandlePromptChanged((string)args.NewValue);
        }

        private static void OnItemsMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue == args.OldValue) return;

            ((Terminal)d)._paragraph.Margin = (Thickness)args.NewValue;
        }

        private static void OnItemHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue == args.OldValue) return;

            ((Terminal)d)._paragraph.LineHeight = (int)args.NewValue;
        }

        private static void OnDisplayPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue == args.OldValue) return;

            ((Terminal)d)._displayPathProperty = null;
        }

        private static void OnLineConverterChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue == args.OldValue)return;

            ((Terminal)d).HandleLineConverterChanged();
        }

        private void CopyCommand(object sender, DataObjectCopyingEventArgs args)
        {
            if (!string.IsNullOrEmpty(Selection.Text))
            {
                args.DataObject.SetData(typeof(string), Selection.Text);
            }
            args.Handled = true;
        }

        private void PasteCommand(object sender, DataObjectPastingEventArgs args)
        {
            var text = args.DataObject.GetData(typeof(string)) as string;

            if (!string.IsNullOrEmpty(text))
            {
                if (Selection.Start != Selection.End)
                {
                    Selection.Start.DeleteTextInRun(Selection.Text.Length);
                    Selection.Start.InsertTextInRun(text);

                    var selectionEnd = Selection.Start.GetPositionAtOffset(text.Length);
                    CaretPosition = selectionEnd;
                }
                else
                {
                    AddLine(text);
                }
            }

            args.CancelCommand();
            args.Handled = true;
        }

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

        private void HandlePromptChanged(string prompt)
        {
            if (_promptInline == null) return;

            _promptInline.Text = prompt;
        }

        private void HandleLineConverterChanged()
        {
            using (DeclareChangeBlock())
            {
                foreach (var run in _paragraph.Inlines.Where(x => x is Run).Cast<Run>())
                {
                    run.Foreground = GetForegroundColor(run.Text);
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
                        ReplaceItems(((IEnumerable) sender).Cast<object>().ToArray());
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
                    
                        newInlines.Add(new Run(line) { Foreground = GetForegroundColor(x) } );
                        newInlines.Add(new LineBreak());
                    }
                    return newInlines;

                }).ToArray();

            _paragraph.Inlines.AddRange(inlines);
            AddPrompt();
            _paragraph.Inlines.Add(new Run(command));
            CaretPosition = CaretPosition.DocumentEnd;
        }

        private Brush GetForegroundColor(object item)
        {
            if (LineColorConverter != null)
            {
                return (Brush)LineColorConverter.Convert(item, typeof(Brush), null, CultureInfo.InvariantCulture);
            }
            return Foreground;
        }

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
        
        private static TextPointer GetTextPointer(TextPointer textPointer, LogicalDirection direction)
        {
            var currentTextPointer = textPointer;
            while (currentTextPointer != null)
            {
                var nextPointer = currentTextPointer.GetNextContextPosition(direction);
                if (nextPointer == null) return null;

                if (nextPointer.GetPointerContext(direction) == TextPointerContext.Text) return nextPointer;

                currentTextPointer = nextPointer;
            }
            return null;
        }

        [NotNull]
        private string ExtractValue(object item)
        {
            if (item == null) return string.Empty;
            var displayPath = ItemDisplayPath;
            if (displayPath == null)
            {
                return (item as string ?? (item as IValue<string>)?.Value) ?? item.ToString();
            }

            if (_displayPathProperty == null)
            {
                _displayPathProperty = item.GetType().GetProperty(displayPath);
            }

            return _displayPathProperty.GetValue(item, null)?.ToString() ?? string.Empty;
        }



        private void OnLineEntered()
        {
            if (LineEntered != null)
            {
                
            }
            LineEntered?.Invoke(this, EventArgs.Empty);
        }

        private void AddLine(string line)
        {
            CaretPosition = CaretPosition.DocumentEnd;

            _paragraph.Inlines.Add(new Run(line));

            CaretPosition = Document.ContentEnd;
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

        private void AddPrompt()
        {
            _paragraph.Inlines.Add(_promptInline);
            _paragraph.Inlines.Add(new Run());

            CaretPosition = Document.ContentEnd;
        }
    }
}
