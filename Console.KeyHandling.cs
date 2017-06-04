using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;

namespace Visyn.Wpf.Console
{
    public partial class Terminal
    {
        /// <summary>
        /// Processes every key pressed when the control has focus.
        /// </summary>
        /// <param name="args">The key pressed arguments.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs args)
        {
            base.OnPreviewKeyDown(args);

            if (args.Key != Key.Tab)
            {
                _currentAutoCompletionList.Clear();
            }
            try
            {
                switch (args.Key)
                {
                    case Key.A:
                        args.Handled = HandleSelectAllKeys();
                        break;
                    case Key.X:
                    case Key.C:
                    case Key.V:
                        args.Handled = HandleCopyKeys(args);
                        break;
                    case Key.Left:
                       args.Handled = HandleLeftKey();
                        break;
                    case Key.Right:
                        break;
                    case Key.PageDown:
                    case Key.PageUp:
                        args.Handled = true;
                        break;
                    case Key.Escape:
                        ClearAfterPrompt();
                        args.Handled = true;
                        break;
                    case Key.Up:
                    case Key.Down:
                        args.Handled = HandleUpDownKeys(args);
                        break;
                    case Key.Delete:
                        args.Handled = HandleDeleteKey();
                        break;
                    case Key.Back:
                        args.Handled = HandleBackspaceKey();
                        break;
                    case Key.Enter:
                        HandleEnterKey();
                        args.Handled = true;
                        break;
                    case Key.Tab:
                        HandleTabKey();
                        args.Handled = true;
                        break;
                    default:
                        args.Handled = HandleAnyOtherKey();
                        break;
                }   
            }
            catch (Exception e)
            {

            }
        }

        #region Key Handlers

        private bool HandleCopyKeys(KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.C:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) return false;

                        var pos = CaretPosition.CompareTo(_promptInline.ContentEnd);
                        var selectionPos = Selection.Start.CompareTo(CaretPosition);

                        return pos < 0 || selectionPos < 0;
                    }
                case Key.X:
                case Key.V:
                {
                    var end = _promptInline.ContentEnd;
                        var pos = CaretPosition.CompareTo(end);
                        var selectionPos = Selection.Start.CompareTo(CaretPosition);

                        return pos < 0 || selectionPos < 0;
                    }
            }
            return false;
        }

        private bool HandleSelectAllKeys()
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Selection.Select(Document.ContentStart, Document.ContentEnd);

                return true;
            }

            return HandleAnyOtherKey();
        }

        private void HandleTabKey()
        {
            if (!_currentAutoCompletionList.Any())
            {
                _currentAutoCompletionList = AutoCompletionsSource?.ToList() ?? new List<string>();
            }

            if (!_currentAutoCompletionList.Any()) return;
            if (_autoCompletionIndex >= _currentAutoCompletionList.Count)
            {
                _autoCompletionIndex = 0;
            }
            ClearAfterPrompt();
            AddLine(_currentAutoCompletionList[_autoCompletionIndex]);
            _autoCompletionIndex++;
        }

        private bool HandleUpDownKeys(KeyEventArgs args)
        {
            var pos = CaretPosition.CompareTo(_promptInline.ContentEnd);

            if (pos < 0) return false;
            if (!_buffer.Any()) return true;

            ClearAfterPrompt();

            string existingLine;
            if (args.Key == Key.Down)
            {
                existingLine = _buffer[_buffer.Count - 1];
                _buffer.RemoveAt(_buffer.Count - 1);
                _buffer.Insert(0, existingLine);
            }
            else
            {
                existingLine = _buffer[0];
                _buffer.RemoveAt(0);
                _buffer.Add(existingLine);
            }

            AddLine(existingLine);

            return true;
        }

        private void HandleEnterKey()
        {
            var line = AggregateAfterPrompt();

            ClearAfterPrompt();

            Line = line;
            _buffer.Insert(0, line);

            CaretPosition = Document.ContentEnd;

            OnLineEntered();
        }

        private bool HandleAnyOtherKey()
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) return false;

                var promptEnd = _promptInline.ContentEnd;

                var pos = CaretPosition.CompareTo(promptEnd);
                return pos < 0;
        }

        private bool HandleBackspaceKey()
        {
            var promptEnd = _promptInline.ContentEnd;

            var textPointer = GetTextPointer(promptEnd, LogicalDirection.Forward);
            if (textPointer == null)
            {
                var pos = CaretPosition.CompareTo(promptEnd);

                if (pos <= 0) return true;
            }
            else
            {
                var pos = CaretPosition.CompareTo(textPointer);
                if (pos <= 0) return true;
            }
            return false;
        }

        private bool HandleLeftKey()
        {
            var promptEnd = _promptInline.ContentEnd;

            var textPointer = GetTextPointer(promptEnd, LogicalDirection.Forward);
            if (textPointer == null)
            {
                if (CaretPosition.CompareTo(promptEnd) == 0) return true;
            }
            else
            {
                if (CaretPosition.CompareTo(textPointer) == 0) return true;
            }
            return false;
        }

        private bool HandleDeleteKey()
        {
            var pos = CaretPosition.CompareTo(_promptInline.ContentEnd);

            return pos < 0;
        }
        #endregion // Key handlers
    }
}
