using System;
using System.Collections.Generic;
using System.Text;
using ZLR.VM;

namespace ZLR.VM.IOFilters
{
    public abstract class FilterBase : IZMachineIO
    {
        protected readonly IZMachineIO next;

        public FilterBase(IZMachineIO next)
        {
            if (next == null)
                throw new ArgumentNullException("primary");

            this.next = next;
        }

        #region IZMachineIO Members

        public virtual string ReadLine(string initial, int time, TimedInputCallback callback, byte[] terminatingKeys, out byte terminator)
        {
            return next.ReadLine(initial, time, callback, terminatingKeys, out terminator);
        }

        public virtual short ReadKey(int time, TimedInputCallback callback, CharTranslator translator)
        {
            return next.ReadKey(time, callback, translator);
        }

        public virtual void PutCommand(string command)
        {
            next.PutCommand(command);
        }

        public virtual void PutChar(char ch)
        {
            next.PutChar(ch);
        }

        public virtual void PutString(string str)
        {
            next.PutString(str);
        }

        public virtual void PutTextRectangle(string[] lines)
        {
            next.PutTextRectangle(lines);
        }

        public virtual bool Buffering
        {
            get
            {
                return next.Buffering;
            }
            set
            {
                next.Buffering = value;
            }
        }

        public virtual bool Transcripting
        {
            get
            {
                return next.Transcripting;
            }
            set
            {
                next.Transcripting = value;
            }
        }

        public virtual void PutTranscriptChar(char ch)
        {
            next.PutTranscriptChar(ch);
        }

        public virtual void PutTranscriptString(string str)
        {
            next.PutTranscriptString(str);
        }

        public virtual System.IO.Stream OpenSaveFile(int size)
        {
            return next.OpenSaveFile(size);
        }

        public virtual System.IO.Stream OpenRestoreFile()
        {
            return next.OpenRestoreFile();
        }

        public virtual System.IO.Stream OpenAuxiliaryFile(string name, int size, bool writing)
        {
            return next.OpenAuxiliaryFile(name, size, writing);
        }

        public virtual System.IO.Stream OpenCommandFile(bool writing)
        {
            return next.OpenCommandFile(writing);
        }

        public virtual void SetTextStyle(TextStyle style)
        {
            next.SetTextStyle(style);
        }

        public virtual void SplitWindow(short lines)
        {
            next.SplitWindow(lines);
        }

        public virtual void SelectWindow(short num)
        {
            next.SelectWindow(num);
        }

        public virtual void EraseWindow(short num)
        {
            next.EraseWindow(num);
        }

        public virtual void EraseLine()
        {
            next.EraseLine();
        }

        public virtual void MoveCursor(short x, short y)
        {
            next.MoveCursor(x, y);
        }

        public virtual void GetCursorPos(out short x, out short y)
        {
            next.GetCursorPos(out x, out y);
        }

        public virtual void SetColors(short fg, short bg)
        {
            next.SetColors(fg, bg);
        }

        public virtual short SetFont(short num)
        {
            return next.SetFont(num);
        }

        public virtual bool DrawCustomStatusLine(string location, short hoursOrScore, short minsOrTurns, bool useTime)
        {
            return next.DrawCustomStatusLine(location, hoursOrScore, minsOrTurns, useTime);
        }

        public virtual void PlaySoundSample(ushort number, SoundAction action, byte volume, byte repeats, SoundFinishedCallback callback)
        {
            next.PlaySoundSample(number, action, volume, repeats, callback);
        }

        public virtual void PlayBeep(bool highPitch)
        {
            next.PlayBeep(highPitch);
        }

        public virtual bool ForceFixedPitch
        {
            get
            {
                return next.ForceFixedPitch;
            }
            set
            {
                next.ForceFixedPitch = value;
            }
        }

        public virtual bool VariablePitchAvailable
        {
            get { return next.VariablePitchAvailable; }
        }

        public virtual bool ScrollFromBottom
        {
            get
            {
                return next.ScrollFromBottom;
            }
            set
            {
                next.ScrollFromBottom = value;
            }
        }

        public virtual bool BoldAvailable
        {
            get { return next.BoldAvailable; }
        }

        public virtual bool ItalicAvailable
        {
            get { return next.ItalicAvailable; }
        }

        public virtual bool FixedPitchAvailable
        {
            get { return next.FixedPitchAvailable; }
        }

        public virtual bool GraphicsFontAvailable
        {
            get { return next.GraphicsFontAvailable; }
        }

        public virtual bool TimedInputAvailable
        {
            get { return next.TimedInputAvailable; }
        }

        public virtual bool SoundSamplesAvailable
        {
            get { return next.SoundSamplesAvailable; }
        }

        public virtual byte WidthChars
        {
            get { return next.WidthChars; }
        }

        public virtual short WidthUnits
        {
            get { return next.WidthUnits; }
        }

        public virtual byte HeightChars
        {
            get { return next.HeightChars; }
        }

        public virtual short HeightUnits
        {
            get { return next.HeightUnits; }
        }

        public virtual byte FontHeight
        {
            get { return next.FontHeight; }
        }

        public virtual byte FontWidth
        {
            get { return next.FontWidth; }
        }

        public virtual event EventHandler SizeChanged
        {
            add { next.SizeChanged += value; }
            remove { next.SizeChanged -= value; }
        }

        public virtual bool ColorsAvailable
        {
            get { return next.ColorsAvailable; }
        }

        public virtual byte DefaultForeground
        {
            get { return next.DefaultForeground; }
        }

        public virtual byte DefaultBackground
        {
            get { return next.DefaultBackground; }
        }

        public virtual UnicodeCaps CheckUnicode(char ch)
        {
            return next.CheckUnicode(ch);
        }

        #endregion
    }
}
