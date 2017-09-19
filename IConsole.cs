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
using System.Windows;
using System.Windows.Data;

namespace Visyn.Wpf.Console
{
    /// <summary>
    /// Exposes the dependancy properties and events exposed by the terminal control.
    /// </summary>
    public interface IConsole
    {
        /// <summary>
        /// Event fired when the user presses the Enter key.
        /// </summary>
        event EventHandler LineEntered;

        /// <summary>
        /// The bound items to the terminal.
        /// </summary>
        IEnumerable ItemsSource { get; set; }

        /// <summary>
        /// Bound autocompletion strings to the terminal.
        /// </summary>
        IEnumerable<string> AutoCompletionsSource { get; set; }

        /// <summary>
        /// The prompt of the terminal.
        /// </summary>
        string Prompt { get; set; }

        /// <summary>
        /// The current editable line of the terminal (bottom line).
        /// </summary>
        string Line { get; set; }

        /// <summary>
        /// The display path for the bound items.
        /// </summary>
        string ItemDisplayPath { get; set; }

        /// <summary>
        /// The error color for the bound items.
        /// </summary>
        IValueConverter LineColorConverter { get; set; }

        /// <summary>
        /// The individual line height for the bound items.
        /// </summary>
        int ItemHeight { get; set; }

        /// <summary>
        /// The margin around the bound items.
        /// </summary>
        Thickness ItemsMargin { get; set; }
    }
}
