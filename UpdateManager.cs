/*
 * Vision.NET 2.1 Computer Vision Library
 * Copyright (C) 2009 Matthew Johnson
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace VisionNET
{
    /// <summary>
    /// Handler for progress update messages.
    /// </summary>
    /// <param name="value">The current progress</param>
    /// <param name="maximum">The maximum amount (when progress is complete)</param>
    public delegate void ProgressUpdate(int value, int maximum);
    /// <summary>
    /// Handler for generic update messages.
    /// </summary>
    /// <param name="message">An update message</param>
    public delegate void MessageUpdate(string message);
    /// <summary>
    /// Handler for console clearing requests.
    /// </summary>
    public delegate void ClearHandler();

    /// <summary>
    /// The Update Manager class provides a mechanism for having console-like feedback that can be handled either
    /// by using the console or by having console-like controls in a GUI.  Essentially, it allows for the same code to be used on the command line
    /// or with a GUI without significant changes to its feedback mechanisms.
    /// </summary>
    public static class UpdateManager
    {
        private static PerformanceCounter _counter = new PerformanceCounter("Memory", "Available MBytes");

        private static bool _appendMemory = true;
        private static bool _consoleOutput = false;

        private static string _tabs = "";

        /// <summary>
        /// Whether the UpdateManager should automatically output all messages to the console.
        /// </summary>
        public static bool ConsoleOutput
        {
            get { return _consoleOutput; }
            set { _consoleOutput = value; }
        }

        /// <summary>
        /// This event fires whenever a process indicates that progress has increased.
        /// </summary>
        public static event ProgressUpdate Progress;
        /// <summary>
        /// This event fires whenever a process indicates that it wants to send a message.
        /// </summary>
        public static event MessageUpdate Message;
        /// <summary>
        /// This event fires whenever a process requests that all previous messages be cleared.
        /// </summary>
        public static event ClearHandler ClearRequest;

        /// <summary>
        /// Whether the UpdateManager should append the currently available memory to all update messages.
        /// </summary>
        public static bool AppendMemory
        {
            get
            {
                return _appendMemory;
            }
            set
            {
                _appendMemory = value;
            }
        }

        /// <summary>
        /// Whether to indent the output messages.
        /// </summary>
        public static bool Indentation { get; set; }

        /// <summary>
        /// The current amount of memory available to the calling process.
        /// </summary>
        public static float MemoryAvailable
        {
            get
            {
                return _counter.NextValue();
            }
        }

        /// <summary>
        /// Adds a level of indentation to the output messages
        /// </summary>
        public static void AddIndent()
        {
            _tabs += '\t';
        }

        /// <summary>
        /// Removes a level of indentation from the output messages
        /// </summary>
        public static void RemoveIndent()
        {
            if (!string.IsNullOrEmpty(_tabs))
                _tabs = _tabs.Substring(0, _tabs.Length - 1);
        }

        /// <summary>
        /// Request a message clear.
        /// </summary>
        public static void Clear()
        {
            if (ClearRequest != null)
                ClearRequest();
        }

        /// <summary>
        /// Request a progress update message be sent.
        /// </summary>
        /// <param name="value">Current progress value</param>
        /// <param name="maximum">Maximum possible progress value</param>
        public static void RaiseProgress(int value, int maximum)
        {
            OnProgress(value, maximum);
            if (Progress != null)
                Progress(value, maximum);
        }

        private static void WriteLine(string message, bool appendNewLine, params object[] args)
        {
            message = string.Format(message, args);
            if (_appendMemory)
                message = string.Format("{0} [{1}]       ", message, _counter.NextValue());
            if (appendNewLine)
                message += "\n";
            if (Indentation)
                message = _tabs + message;
            OnMessage(message);
            if (Message != null)
            {
                Message(message);
            }
        }

        /// <summary>
        /// Emulates <see cref="Console.WriteLine(string)"/>, but generates a message update request instead.
        /// </summary>
        /// <param name="o">Object to write</param>
        public static void WriteLine(object o)
        {
            WriteLine(o.ToString(), true);
        }

        /// <summary>
        /// Emulates <see cref="Console.WriteLine(string,object)"/>, but generates a message update request instead.
        /// </summary>
        /// <param name="message">A format string</param>
        /// <param name="args">Arguments to the format string</param>
        public static void WriteLine(string message, params object[] args)
        {
            WriteLine(message, true, args);
        }

        /// <summary>
        /// Writes a blank line.
        /// </summary>
        public static void WriteLine()
        {
            WriteLine("", true);
        }

        /// <summary>
        /// Emulates <see cref="Console.Write(string)"/>, but generates a message update request instead.
        /// </summary>
        /// <param name="o">Object to write</param>
        public static void Write(object o)
        {
            WriteLine(o.ToString(), false);
        }

        /// <summary>
        /// Emulates <see cref="Console.Write(string,object)"/>, but generates a message update request instead.
        /// </summary>
        /// <param name="message">A format string</param>
        /// <param name="args">Arguments to the format string</param>
        public static void Write(string message, params object[] args)
        {
            WriteLine(message, false, args);
        }

        /// <summary>
        /// Creates an enumerable object which generates a progress update each time a new 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static IEnumerable<T> ProgressEnum<T>(this IEnumerable<T> items)
        {
            int index = 0;
            int total = items.Count();
            foreach(var item in items){
                RaiseProgress(++index, total);
                yield return item;
            }
        }

        /// <summary>
        /// Creates an enumerable object which generates a progress update each time a new 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reportFrequency">Rate at which a progress event will be fired (i.e. every N increments)</param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static IEnumerable<T> ProgressEnum<T>(IEnumerable<T> items, int reportFrequency)
        {
            int index = 0;
            int total = items.Count();
            foreach (var item in items)
            {
                index++;
                if(index%reportFrequency == 0)
                    RaiseProgress(index, total);
                yield return item;
            }
        }

        private static bool _lastProgress;
        private static void OnMessage(string message)
        {
            if (ConsoleOutput)
            {
                if (_lastProgress)
                {
                    Console.WriteLine();
                    _lastProgress = false;
                }
                Console.Write(message);
            }
        }

        private static void OnProgress(int value, int maximum)
        {
            if (ConsoleOutput)
            {
                string message = string.Format("\r{0:0000}/{1:0000}", value, maximum);
                if(AppendMemory)
                    message += " ["+MemoryAvailable+"]";
                Console.Write(message);
                _lastProgress = true;
            }
        }

    }
}
