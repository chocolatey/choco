// Copyright © 2017 - 2018 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
// You may obtain a copy of the License at
// 
// 	http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.adapters
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using app;
    using commandline;
    using platforms;

    /// <summary>
    /// Adapter for System.Console
    /// </summary>
    public sealed class Console : IConsole
    {
        public string ReadLine()
        {
            if (!ApplicationParameters.AllowPrompts) return string.Empty;

            return System.Console.ReadLine();
        }

        public string ReadLine(int timeoutMilliseconds)
        {
            if (!ApplicationParameters.AllowPrompts) return string.Empty;

            return ReadLineTimeout.read(timeoutMilliseconds);
        }

        public System.ConsoleKeyInfo ReadKey(bool intercept)
        {
            if (!ApplicationParameters.AllowPrompts) return new System.ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false);

            return System.Console.ReadKey(intercept);
        }

        public System.ConsoleKeyInfo ReadKey(int timeoutMilliseconds)
        {
            if (!ApplicationParameters.AllowPrompts) return new System.ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false);

            return ReadKeyTimeout.read_key(timeoutMilliseconds);
        }

        public TextWriter Error { get { return System.Console.Error; } }

        public TextWriter Out { get { return System.Console.Out; } }

        public void Write(object value)
        {
            System.Console.Write(value.to_string());
        }

        public void WriteLine()
        {
            System.Console.WriteLine();
        }

        public void WriteLine(object value)
        {
            System.Console.WriteLine(value);
        }

        public System.ConsoleColor BackgroundColor
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.BackgroundColor;

                return System.ConsoleColor.Black;
            }
            set
            {
                if (!IsOutputRedirected) System.Console.BackgroundColor = value;
            }
        }

        public System.ConsoleColor ForegroundColor
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.ForegroundColor;

                return System.ConsoleColor.Gray;
            }
            set
            {
                if (!IsOutputRedirected) System.Console.ForegroundColor = value;
            }
        }

        public int BufferWidth
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.BufferWidth;

                return get_console_buffer().dwSize.X; //the current console window width
            }
            set
            {
                if (!IsOutputRedirected) System.Console.BufferWidth = value;
            }
        }

        public int BufferHeight
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.BufferHeight;

                return get_console_buffer().dwSize.Y; //the current console window height
            }
            set
            {
                if (!IsOutputRedirected) System.Console.BufferHeight = value;
            }
        }

        public void SetBufferSize(int width, int height)
        {
            if (!IsOutputRedirected) System.Console.SetBufferSize(width, height);
        }

        public string Title
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.Title;

                return string.Empty;
            }
            set
            {
                if (!IsOutputRedirected) System.Console.Title = value;
            }
        }

        public bool KeyAvailable
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.KeyAvailable;

                return false;
            }
        }

        public int CursorSize
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.CursorSize;

                return get_console_buffer().dwCursorPosition.Y;
            }
            set
            {
                if (!IsOutputRedirected) System.Console.CursorSize = value;
            }
        }

        public int LargestWindowWidth
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.LargestWindowWidth;

                return get_console_buffer().dwMaximumWindowSize.X; //the max console window width
            }
        }

        public int LargestWindowHeight
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.LargestWindowHeight;

                return get_console_buffer().dwMaximumWindowSize.Y; //the max console window height
            }
        }

        public int WindowWidth
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.WindowWidth;

                return get_console_buffer().dwSize.X; //the current console window width
            }
            set
            {
                if (!IsOutputRedirected) System.Console.WindowWidth = value;
            }
        }

        public int WindowHeight
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.WindowHeight;

                return get_console_buffer().dwSize.Y; //the current console window height
            }
            set
            {
                if (!IsOutputRedirected) System.Console.WindowHeight = value;
            }
        }

        public void SetWindowSize(int width, int height)
        {
            if (!IsOutputRedirected) System.Console.SetWindowSize(width, height);
        }

        public int WindowLeft
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.WindowLeft;

                return get_console_buffer().srWindow.Left;
            }
            set
            {
                if (!IsOutputRedirected) System.Console.WindowLeft = value;
            }
        }

        public int WindowTop
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.WindowTop;

                return get_console_buffer().srWindow.Top;
            }
            set
            {
                if (!IsOutputRedirected) System.Console.WindowTop = value;
            }
        }

        public void SetWindowPosition(int width, int height)
        {
            if (!IsOutputRedirected) System.Console.SetWindowPosition(width, height);
        }

        /// <remarks>
        /// Based on http://stackoverflow.com/a/20737289/18475 / http://stackoverflow.com/a/3453272/18475
        /// </remarks>
        public bool IsOutputRedirected
        {
            get
            {
                if (!is_windows()) return false;

                return FileType.Char != GetFileType(GetStdHandle(StdHandle.StdOut));
            }
        }

        /// <remarks>
        /// Based on http://stackoverflow.com/a/20737289/18475 / http://stackoverflow.com/a/3453272/18475
        /// </remarks>
        public bool IsErrorRedirected
        {
            get
            {
                if (!is_windows()) return false;

                return FileType.Char != GetFileType(GetStdHandle(StdHandle.StdErr));
            }
        }

        /// <remarks>
        /// Based on http://stackoverflow.com/a/20737289/18475 / http://stackoverflow.com/a/3453272/18475
        /// </remarks>
        public bool IsInputRedirected
        {
            get
            {
                if (!is_windows()) return false;

                return FileType.Char != GetFileType(GetStdHandle(StdHandle.StdIn));
            }
        }

        private bool is_windows()
        {
            return Platform.get_platform() == PlatformType.Windows;
        }

        private CONSOLE_SCREEN_BUFFER_INFO get_console_buffer()
        {
            var defaultConsoleBuffer = new CONSOLE_SCREEN_BUFFER_INFO
            {
                dwSize = new COORD(),
                dwCursorPosition = new COORD(),
                dwMaximumWindowSize = new COORD(),
                srWindow = new SMALL_RECT(),
                wAttributes = 0,
            };

            if (!is_windows()) return defaultConsoleBuffer;

            CONSOLE_SCREEN_BUFFER_INFO csbi;
            if (GetConsoleScreenBufferInfo(GetStdHandle(StdHandle.StdOut), out csbi))
            {
                // if the console buffer exists
                return csbi;
            }

            return defaultConsoleBuffer;
        }

        /// <summary>
        /// Contains information about a console screen buffer.
        /// </summary>
        /// <remarks>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms682093.aspx
        /// </remarks>
        private struct CONSOLE_SCREEN_BUFFER_INFO
        {
            /// <summary> A CoOrd structure that contains the size of the console screen buffer, in character columns and rows. </summary>
            internal COORD dwSize;
            /// <summary> A CoOrd structure that contains the column and row coordinates of the cursor in the console screen buffer. </summary>
            internal COORD dwCursorPosition;
            /// <summary> The attributes of the characters written to a screen buffer by the WriteFile and WriteConsole functions, or echoed to a screen buffer by the ReadFile and ReadConsole functions. </summary>
            internal System.Int16 wAttributes;
            /// <summary> A SmallRect structure that contains the console screen buffer coordinates of the upper-left and lower-right corners of the display window. </summary>
            internal SMALL_RECT srWindow;
            /// <summary> A CoOrd structure that contains the maximum size of the console window, in character columns and rows, given the current screen buffer size and font and the screen size. </summary>
            internal COORD dwMaximumWindowSize;
        }

        /// <summary>
        /// Defines the coordinates of a character cell in a console screen buffer. 
        /// The origin of the coordinate system (0,0) is at the top, left cell of the buffer.
        /// </summary>
        /// <remarks>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms682119.aspx
        /// </remarks>
        private struct COORD
        {
            /// <summary> The horizontal coordinate or column value. </summary>
            internal System.Int16 X;
            /// <summary> The vertical coordinate or row value. </summary>
            internal System.Int16 Y;
        }

        /// <summary>
        /// Defines the coordinates of the upper left and lower right corners of a rectangle.
        /// </summary>
        /// <remarks>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms686311.aspx
        /// </remarks>
        private struct SMALL_RECT
        {
            /// <summary> The x-coordinate of the upper left corner of the rectangle. </summary>
            internal System.Int16 Left;
            /// <summary> The y-coordinate of the upper left corner of the rectangle. </summary>
            internal System.Int16 Top;
            /// <summary> The x-coordinate of the lower right corner of the rectangle. </summary>
            internal System.Int16 Right;
            /// <summary> The y-coordinate of the lower right corner of the rectangle. </summary>
            internal System.Int16 Bottom;
        }

        private enum StdHandle
        {
            StdIn = -10,
            StdOut = -11,
            StdErr = -12,
        };

        /// <summary>
        /// Retrieves information about the specified console screen buffer.
        /// </summary>
        /// <returns>
        /// If the information retrieval succeeds, the return value is nonzero; else the return value is zero
        /// </returns>
        /// <remarks>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms683171.aspx
        /// </remarks>
        [DllImport("kernel32.dll", EntryPoint = "GetConsoleScreenBufferInfo", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetConsoleScreenBufferInfo(System.IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        /// <summary>
        /// Retrieves a handle to the specified standard device (standard input, standard output, or standard error).
        /// </summary>
        /// <returns>
        /// Returns a value that is a handle to the specified device, or a redirected handle
        /// </returns>
        /// <remarks>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms683231.aspx
        /// </remarks>
        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true)]
        private static extern System.IntPtr GetStdHandle(StdHandle nStdHandle);

        /// <summary>
        /// Retrieves the file type of the specified file.
        /// </summary>
        /// <remarks>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa364960.aspx 
        /// http://www.pinvoke.net/default.aspx/kernel32.getfiletype
        /// </remarks>
        [DllImport("kernel32.dll")]
        private static extern FileType GetFileType(System.IntPtr hFile);

        /// <remarks>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa364960.aspx
        /// </remarks>
        private enum FileType
        {
            /// <summary> Either the type of the specified file is unknown, or the function failed. </summary>
            Unknown,
            /// <summary> The specified file is a disk file. </summary>
            Disk,
            /// <summary> The specified file is a character file, typically an LPT device or a console. </summary>
            Char,
            /// <summary> The specified file is a socket, a named pipe, or an anonymous pipe. </summary>
            Pipe
        };

    }
}
