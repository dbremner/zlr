using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZLR.VM.IOFilters
{
    public sealed class InternalSaveFilter : FilterBase
    {
        private MemoryStream saveData;

        public InternalSaveFilter(IZMachineIO next)
            : base(next)
        {
        }

        public override System.IO.Stream OpenSaveFile(int size)
        {
            saveData = new MemoryStream(size);
            return saveData;
        }

        public override Stream OpenRestoreFile()
        {
            if (saveData != null)
                return new MemoryStream(saveData.ToArray(), false);

            return null;
        }
    }
}
