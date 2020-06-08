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

namespace chocolatey.infrastructure.powershell
{
    using System;
    using System.Management.Automation.Host;
    using adapters;
    using Console = adapters.Console;

    public class PoshHostRawUserInterface : PSHostRawUserInterface
    {
        private static readonly Lazy<IConsole> _console = new Lazy<IConsole>(() => new Console());
        private static IConsole Console { get { return _console.Value; } }

        public override ConsoleColor BackgroundColor
        {
            get { return Console.BackgroundColor; } 
            set { Console.BackgroundColor = value; }
        }

        public override ConsoleColor ForegroundColor
        {
            get { return Console.ForegroundColor; } 
            set { Console.ForegroundColor = value; }
        }

        public override Size BufferSize
        {
            get { return new Size(Console.BufferWidth, Console.BufferHeight); } 
            set { Console.SetBufferSize(value.Width, value.Height); }
        }

        public override Coordinates CursorPosition { get; set; }

        public override int CursorSize
        {
            get { return Console.CursorSize; } 
            set { Console.CursorSize = value; }
        }

        public override bool KeyAvailable
        {
            get { return Console.KeyAvailable; }
        }

        public override Size MaxPhysicalWindowSize
        {
            get { return new Size(Console.LargestWindowWidth, Console.LargestWindowHeight); }
        }

        public override Size MaxWindowSize
        {
            get { return new Size(Console.LargestWindowWidth, Console.LargestWindowHeight); }
        }

        public override Coordinates WindowPosition {
            get { return new Coordinates(Console.WindowLeft, Console.WindowTop); } 
            set { Console.SetWindowPosition(value.X, value.Y); }
        }

        public override Size WindowSize
        {
            get { return new Size(Console.WindowWidth, Console.WindowHeight); } 
            set { Console.SetWindowSize(value.Width, value.Height); }
        }

        public override string WindowTitle
        {
            get { return Console.Title; } 
            set { Console.Title = value; }
        }

        #region Not Implemented / Empty

        public override void FlushInputBuffer()
        {
        }

        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            throw new NotImplementedException();
        }

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            throw new NotImplementedException();
        }

        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            throw new NotImplementedException();
        }

        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            throw new NotImplementedException();
        }

        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
