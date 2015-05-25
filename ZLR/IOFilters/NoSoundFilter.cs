using System;
using System.Collections.Generic;
using System.Text;

namespace ZLR.VM.IOFilters
{
    public sealed class NoSoundFilter : FilterBase
    {
        public override void PlayBeep(bool highPitch)
        {
            // nada
        }

        public override void PlaySoundSample(ushort number, SoundAction action, byte volume, byte repeats, SoundFinishedCallback callback)
        {
            // nada
        }
    }
}
