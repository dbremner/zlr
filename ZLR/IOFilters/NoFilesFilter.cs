using System;
using System.Collections.Generic;
using System.Text;

namespace ZLR.VM.IOFilters
{
    public sealed class NoFilesFilter : FilterBase
    {
        public override System.IO.Stream OpenAuxiliaryFile(string name, int size, bool writing)
        {
            return null;
        }

        public override System.IO.Stream OpenCommandFile(bool writing)
        {
            return null;
        }

        public override System.IO.Stream OpenRestoreFile()
        {
            return null;
        }

        public override System.IO.Stream OpenSaveFile(int size)
        {
            return null;
        }
    }
}
