﻿using CTFAK.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTFAK.MFA.MFAObjectLoaders
{
    public class MFAActive : MFAAnimationObject
    {
 

        public override void Read()
        {
            base.Read();
        }

        public override void Write(ByteWriter Writer)
        {
            base.Write(Writer);
        }

        public MFAActive(ByteReader reader) : base(reader) { }
    }
}
