using System;
using System.IO;
using System.Threading;
using ZLR.VM;

namespace ZLR.Interfaces.SystemConsole
{
    internal class ConsoleIO : IZMachineIO
    {
        private readonly string fileBase;
        private int split = 0;
        private bool upper = false;
        private int xupper = 1, yupper = 1, xlower = 1, ylower = 1;
        private ConsoleColor bgupper = ConsoleColor.Black, fgupper = ConsoleColor.Gray;
        private ConsoleColor bglower = ConsoleColor.Black, fglower = ConsoleColor.Gray;
        private bool reverse, emphasis;

        private int origBufHeight;

        public ConsoleIO(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileBase");

            fileBase = Path.GetFileName(fileName);

            origBufHeight = Console.BufferHeight;
            Console.BufferWidth = Console.WindowWidth;

            Console.Title = fileName + " - ConsoleZLR";
        }

        public string ReadLine(int time, TimedInputCallback callback,
            byte[] terminatingKeys, out byte terminator)
        {
            // can't do timed input with Console.ReadLine...
            //XXX handle terminating keys and timed input (this means replacing Console.ReadLine())
            terminator = 13;
            return Console.ReadLine();
        }

        public short ReadKey(int time, TimedInputCallback callback, CharTranslator translator)
        {
            if (time > 0)
            {
                int sleeps = 0;
                while (!Console.KeyAvailable)
                {
                    Thread.Sleep(100);
                    if (Console.KeyAvailable)
                        break;

                    sleeps++;
                    if (sleeps == time)
                    {
                        sleeps = 0;
                        if (callback() == true)
                            return 0;
                    }
                }
            }

            ConsoleKeyInfo info = Console.ReadKey(true);
            switch (info.Key)
            {
                case ConsoleKey.Delete: return 8;
                case ConsoleKey.Tab: return 9;
                case ConsoleKey.Enter: return 13;
                case ConsoleKey.Escape: return 27;
                case ConsoleKey.UpArrow: return 129;
                case ConsoleKey.DownArrow: return 130;
                case ConsoleKey.LeftArrow: return 131;
                case ConsoleKey.RightArrow: return 132;
                case ConsoleKey.F1: return 133;
                case ConsoleKey.F2: return 134;
                case ConsoleKey.F3: return 135;
                case ConsoleKey.F4: return 136;
                case ConsoleKey.F5: return 137;
                case ConsoleKey.F6: return 138;
                case ConsoleKey.F7: return 139;
                case ConsoleKey.F8: return 140;
                case ConsoleKey.F9: return 141;
                case ConsoleKey.F10: return 142;
                case ConsoleKey.F11: return 143;
                case ConsoleKey.F12: return 144;
                case ConsoleKey.NumPad0: return 145;
                case ConsoleKey.NumPad1: return 146;
                case ConsoleKey.NumPad2: return 147;
                case ConsoleKey.NumPad3: return 148;
                case ConsoleKey.NumPad4: return 149;
                case ConsoleKey.NumPad5: return 150;
                case ConsoleKey.NumPad6: return 151;
                case ConsoleKey.NumPad7: return 152;
                case ConsoleKey.NumPad8: return 153;
                case ConsoleKey.NumPad9: return 154;
                default: return translator(info.KeyChar);
            }
        }

        public bool ReadingCommandsFromFile
        {
            get { return false; }
            set { /* nada */ }
        }

        public bool WritingCommandsToFile
        {
            get { return false; }
            set { /* nada */ }
        }

        public void PutChar(char ch)
        {
            Console.Write(ch);
        }

        public void PutString(string str)
        {
            Console.Write(str);
        }

        public void PutTextRectangle(string[] lines)
        {
            int row = Console.CursorTop;
            int col = Console.CursorLeft;

            foreach (string line in lines)
            {
                Console.SetCursorPosition(col, row++);
                Console.Write(line);
            }
        }

        private void SetConsoleColors()
        {
            ConsoleColor bg, fg;
            if (upper)
            {
                bg = bgupper;
                fg = fgupper;
            }
            else
            {
                bg = bglower;
                fg = fglower;
            }

            if (emphasis)
                fg = EmphasizeColor(fg);

            if (reverse)
            {
                ConsoleColor temp = bg;
                bg = fg;
                fg = temp;
            }

            Console.BackgroundColor = bg;
            Console.ForegroundColor = fg;
        }

        public void SetTextStyle(short style)
        {
            switch (style)
            {
                case 0:
                    reverse = false;
                    emphasis = false;
                    break;

                case 1:
                    reverse = true;
                    break;

                case 2:
                case 4:
                    emphasis = true;
                    break;
            }

            SetConsoleColors();
        }

        public void SplitWindow(short lines)
        {
            split = Math.Min(lines, Console.WindowHeight);
            if (split == 0)
                Console.BufferHeight = origBufHeight;
            else
                Console.BufferHeight = Console.WindowHeight;
        }

        public void SelectWindow(short num)
        {
            SaveCursorPos();

            switch (num)
            {
                case 0:
                    upper = false;
                    break;

                case 1:
                    upper = true;
                    break;

                default:
                    return;
            }

            RestoreCursorPos();
            SetConsoleColors();
        }

        private void RestoreCursorPos()
        {
            if (upper)
                Console.SetCursorPosition(xupper - 1, yupper - 1);
            else
                Console.SetCursorPosition(xlower - 1, ylower - 1 + split);
        }

        private void SaveCursorPos()
        {
            if (upper)
            {
                xupper = Console.CursorLeft + 1;
                yupper = Console.CursorTop + 1;
            }
            else
            {
                xlower = Console.CursorLeft + 1;
                ylower = Console.CursorTop - split + 1;
            }
        }

        public void EraseWindow(short num)
        {
            if (num < 0)
            {
                // -1 = erase all and unsplit, -2 = erase all but keep split
                // both select the lower window and move its cursor to the top left
                Console.Clear();

                if (num == -1)
                    split = 0;

                upper = false;
                xlower = 1;
                ylower = 1;
                Console.SetCursorPosition(xlower - 1, ylower - 1 + split);
                return;
            }

            SaveCursorPos();

            if (num == 0)
            {
                // erase lower
                int height = Console.WindowHeight - split;
                int width = Console.WindowWidth;
                Console.BackgroundColor = bglower;
                for (int i = 0; i < height; i++)
                {
                    Console.SetCursorPosition(0, split + i);
                    for (int j = 0; j < width; j++)
                        Console.Write(' ');
                }
                xlower = 1;
                ylower = 1;
            }
            else if (num == 1)
            {
                // erase upper
                int height = split;
                int width = Console.WindowWidth;
                Console.BackgroundColor = bgupper;
                for (int i = 0; i < height; i++)
                {
                    Console.SetCursorPosition(0, i);
                    for (int j = 0; j < width; j++)
                        Console.Write(' ');
                }
                xupper = 1;
                yupper = 1;
            }

            // restore colors and cursor
            RestoreCursorPos();
            SetConsoleColors();
        }

        public void EraseLine()
        {
            SaveCursorPos();

            int count = Console.WindowWidth - Console.CursorLeft;
            for (int i = 0; i < count; i++)
                Console.Write(' ');

            RestoreCursorPos();
        }

        public void MoveCursor(short x, short y)
        {
            // only allowed when upper window is selected
            if (upper)
            {
                if (x < 1)
                    x = 1;
                else if (x > Console.WindowWidth)
                    x = (short)Console.WindowWidth;

                if (y < 1)
                    y = 1;

                if (y > split)
                    y = (short)split;
                Console.SetCursorPosition(x - 1, y - 1);
            }
        }

        public void GetCursorPos(out short x, out short y)
        {
            int cx = Console.CursorLeft;
            int cy = Console.CursorTop;

            if (upper)
            {
                x = (short)(cx + 1);
                y = (short)(cy + 1);
            }
            else
            {
                x = (short)(cx + 1);
                y = (short)(cy + 1 - split);
            }
        }

        public bool ForceFixedPitch
        {
            get { return true; }
            set { /* nada */ }
        }

        public bool BoldAvailable
        {
            get { return true; }
        }

        public bool ItalicAvailable
        {
            get { return true; }
        }

        public bool FixedPitchAvailable
        {
            get { return false; }
        }

        public bool TimedInputAvailable
        {
            get { return false; }
        }

        public bool Transcripting
        {
            get { return false; }
            set { /* nada */}
        }

        public void PutTranscriptChar(char ch)
        {
            // nada
        }

        public void PutTranscriptString(string str)
        {
            // nada
        }

        public void SetColors(short fg, short bg)
        {
            if (upper)
            {
                fgupper = ColorToConsole(fg, fgupper, false);
                bgupper = ColorToConsole(bg, bgupper, true);
            }
            else
            {
                fglower = ColorToConsole(fg, fglower, false);
                bglower = ColorToConsole(bg, bglower, true);
            }

            SetConsoleColors();
        }

        /*
            0  =  the current setting of this colour
            1  =  the default setting of this colour
            2  =  black   3 = red       4 = green    5 = yellow
            6  =  blue    7 = magenta   8 = cyan     9 = white
         */
        private ConsoleColor ColorToConsole(short num, ConsoleColor current, bool background)
        {
            switch (num)
            {
                case 0: return current;
                case 1: return background ? ConsoleColor.Black : ConsoleColor.Gray;
                case 2: return ConsoleColor.Black;
                case 3: return ConsoleColor.DarkRed;
                case 4: return ConsoleColor.DarkGreen;
                case 5: return ConsoleColor.DarkYellow;
                case 6: return ConsoleColor.DarkBlue;
                case 7: return ConsoleColor.DarkMagenta;
                case 8: return ConsoleColor.DarkCyan;
                case 9: return ConsoleColor.Gray;
                default:
                    return current;
            }
        }

        private ConsoleColor EmphasizeColor(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Black: return ConsoleColor.DarkGray;
                case ConsoleColor.DarkRed: return ConsoleColor.Red;
                case ConsoleColor.DarkGreen: return ConsoleColor.Green;
                case ConsoleColor.DarkYellow: return ConsoleColor.Yellow;
                case ConsoleColor.DarkBlue: return ConsoleColor.Blue;
                case ConsoleColor.DarkMagenta: return ConsoleColor.Magenta;
                case ConsoleColor.DarkCyan: return ConsoleColor.Cyan;
                case ConsoleColor.Gray: return ConsoleColor.White;
                default:
                    return color;
            }
        }

        public byte WidthChars
        {
            get { return (byte)Console.WindowWidth; }
        }

        public short WidthUnits
        {
            get { return (short)Console.WindowWidth; }
        }

        public byte HeightChars
        {
            get { return (byte)Console.WindowHeight; }
        }

        public short HeightUnits
        {
            get { return (short)Console.WindowHeight; }
        }

        public byte FontHeight
        {
            get { return 1; }
        }

        public byte FontWidth
        {
            get { return 1; }
        }

        public event EventHandler SizeChanged
        {
            add { /* nada */ }
            remove { /* nada */ }
        }

        public bool ColorsAvailable
        {
            get { return true; }
        }

        public byte DefaultForeground
        {
            get { return 9; /* white */ }
        }

        public byte DefaultBackground
        {
            get { return 2; /* black */ }
        }

        public Stream OpenSaveFile(int size)
        {
            string defaultFile = fileBase + ".sav";

            string filename = null;
            do
            {
                Console.Write("Enter a new saved game file (\".\" to quit) [{0}]: ",
                    defaultFile);
                filename = Console.ReadLine();
                if (filename == "")
                    filename = defaultFile;

                if (filename == ".")
                    return null;

                if (File.Exists(filename))
                {
                    string yorn;
                    do
                    {
                        Console.Write("\"{0}\" exists. Are you sure (y/n)? ", filename);
                        yorn = Console.ReadLine().ToLower().Trim();
                    }
                    while (yorn.Length == 0);

                    if (yorn[0] != 'y')
                        filename = null;
                }
            }
            while (filename == null);

            return new FileStream(filename, FileMode.Create, FileAccess.Write);
        }

        public Stream OpenRestoreFile()
        {
            string filename;
            do
            {
                Console.Write("Enter an existing saved game file (blank to quit): ");
                filename = Console.ReadLine();
                if (filename == "")
                    return null;

                if (File.Exists(filename))
                    break;
            }
            while (true);

            return new FileStream(filename, FileMode.Open, FileAccess.Read);
        }

        public Stream OpenAuxiliaryFile(string name, int size, bool writing)
        {
            // TODO: support auxiliary files in ConsoleIO
            return null;
        }

        public short SetFont(short num)
        {
            // no font changes supported
            return 0;
        }

        public void PlaySoundSample(ushort num, short effect, byte volume, byte repeats,
            SoundFinishedCallback callback)
        {
            // not supported
        }

        public void PlayBeep(bool highPitch)
        {
            Console.Beep(highPitch ? 1600 : 800, 200);
        }

        public bool SoundSamplesAvailable
        {
            get { return false; }
        }

        // XXX support text buffering in ConsoleIO
        public bool Buffering
        {
            get { return false; }
            set { /* nada */ }
        }

        public UnicodeCaps CheckUnicode(char ch)
        {
            // naive
            return UnicodeCaps.CanInput | UnicodeCaps.CanPrint;
        }
    }
}