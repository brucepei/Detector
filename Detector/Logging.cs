﻿namespace Detector
{
    using System;
    using System.Windows.Media;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.IO;

    public delegate void UpdateLogDelegate(String msg, Color color);
    public abstract class LogWindow : Window
    {
        public abstract RichTextBox LogBox
        {
            get;
        }

        public UpdateLogDelegate UpdateLogBox;
        public LogWindow()
        {
            UpdateLogBox = new UpdateLogDelegate(toLogBox);
        }

        public void toLogBox(String msg, Color color)
        {
            if (LogBox != null)
            {
                bool focused = LogBox.IsFocused;
                if (!focused)
                {
                    LogBox.Focus();
                }
                var para = new Paragraph { Margin = new Thickness(0) };
                LogBox.Document.Blocks.Add(para);
                Run run = new Run() { Text = msg, Foreground = new SolidColorBrush(color) };
                para.Inlines.Add(run);
                LogBox.ScrollToEnd();
            }
        }
    }

    public static class ExceptionExtensions
    {
        public static Exception GetOriginalException(this Exception ex)
        {
            if (ex.InnerException == null) return ex;

            return ex.InnerException.GetOriginalException();
        }

        public static String GetOriginalMessage(this Exception ex)
        {
            var orig = ex.GetOriginalException();
            return String.Format("{0} threw {1}: {2}", orig.TargetSite.Name, orig.GetType().ToString(), orig.Message);
        }
    }

    public enum LogType
    {
        DEBUG,
        INFO,
        NOTE,
        WARNING,
        ERROR
    }

    public class Logging
    {
        public static bool deaf;
        public static string exceptionsPerRun;
        public static bool isRecording;
        public static bool LogConsole = true;
        public static string logPerRun;
        public static DateTime startPerRun;
        public static LogWindow ui;
        public static LogType level = LogType.INFO;

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();
        private const UInt32 StdOutputHandle = 0xFFFFFFF5;
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(UInt32 nStdHandle);
        [DllImport("kernel32.dll")]
        private static extern void SetStdHandle(UInt32 nStdHandle, IntPtr handle);

        public static void Initialize(Boolean openConsole, Boolean resetStdout)
        {
            if (openConsole)
            {
                AllocConsole();
            }
            else
            {
                AttachConsole(-1);
            }
            if (resetStdout)
            {
                // stdout's handle seems to always be equal to 7
                IntPtr defaultStdout = new IntPtr(7);
                IntPtr currentStdout = GetStdHandle(StdOutputHandle);
                if (currentStdout != defaultStdout)
                    SetStdHandle(StdOutputHandle, defaultStdout);
                TextWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                Console.SetOut(writer);
            }
        }

        public static void Initialize(LogWindow targetWindow)
        {
            ui = targetWindow;
            if (ui != null)
            {
                ui.LogBox.Document.Blocks.Clear();
            }
        }

        public static void Initialize(LogWindow targetWindow, Boolean openConsole, Boolean resetStdout)
        {
            Initialize(targetWindow);
            Initialize(openConsole, resetStdout);
        }

        private static void log(string msg, LogType type = LogType.INFO)
        {
            msg = string.Format("{0:HH:mm:ss.fff}  {1}", DateTime.Now, msg);
            //if ((ui != null) && ui.IsLoaded) //it would cause child thread exception: ui.IsLoaded cannot be accessed due to another thread owned
            if (ui != null)
            {
                object[] objArray;
                if (type == LogType.ERROR)
                {
                    objArray = new object[] { msg, (Color)ColorConverter.ConvertFromString("Red") };
                }
                else if (type == LogType.NOTE)
                {
                    objArray = new object[] { msg, (Color)ColorConverter.ConvertFromString("Blue") };
                }
                else if (type == LogType.WARNING)
                {
                    objArray = new object[] { msg, (Color)ColorConverter.ConvertFromString("DarkOrange") };
                }
                else if (type == LogType.INFO)
                {
                    objArray = new object[] { msg, (Color)ColorConverter.ConvertFromString("DarkGreen") };
                }
                else
                {
                    objArray = new object[] { msg, (Color)ColorConverter.ConvertFromString("Black") };
                }
                try
                {
                    //ui.Dispatcher.Invoke(ui.UpdateLogBox, objArray); //will start a new thread and block until Dispatcher handled all other delegates
                    ui.Dispatcher.BeginInvoke(ui.UpdateLogBox, objArray); //some log lines may be misordered
                }
                catch (Exception exception)
                {
                    logException(exception);
                }
            }
            if (LogConsole)
            {
                Console.WriteLine(msg);
            }
            if (isRecording)
            {
                string str = string.Empty;
                switch (type)
                {
                    case LogType.INFO:
                        str = "__cfg__ ";
                        break;

                    case LogType.ERROR:
                        str = "__cfr__ ";
                        break;

                    case LogType.WARNING:
                        str = "__cfy__ ";
                        break;

                    case LogType.NOTE:
                        str = "__cfb__ ";
                        break;

                    default:
                        str = "__cfg__ ";
                        break;
                }
                logPerRun = logPerRun + str + msg + Environment.NewLine;
            }
        }

        public static void logException(Exception ex)
        {
            Console.WriteLine(ex.ToString());
            exceptionsPerRun = exceptionsPerRun + ex.ToString();
            exceptionsPerRun = exceptionsPerRun + "\r\n===============================================\r\n\r\n";
        }

        public static void logMessage(object obj)
        {
            var ex = obj as Exception;
            if (ex != null)
            {
                logMessage(ex.GetOriginalException().ToString(), LogType.ERROR, 0);
            }
            else if(obj != null)
            {
                logMessage(obj.ToString(), LogType.INFO, 0);
            }
            else
            {
                logMessage("Null", LogType.INFO, 0);
            }
        }

        public static void logMessage(string msg, object obj)
        {
            logMessage(msg, LogType.INFO, 0);
            logMessage(obj);
        }

        public static void logMessage(string msg, LogType type = LogType.INFO, int indent_level = 0)
        {
            if (type < level)
                return;
            if (!deaf)
            {
                string str = string.Empty;
                for (int i = 0; i < indent_level; i++)
                {
                    str = str + "\t";
                }
                msg = str + msg;
                log(msg, type);
            }
        }

    }
}

