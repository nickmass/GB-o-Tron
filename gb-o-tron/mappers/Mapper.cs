using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace gb_o_tron.mappers
{
    public abstract class Mapper
    {
        public GBCore gb;
        public virtual void Power() { }
        public virtual byte Read(byte value, ushort address) { return value; }
        public virtual void Write(byte value, ushort address) { }
        public virtual void StateSave(BinaryWriter writer) { }
        public virtual void StateLoad(BinaryReader reader) { }
    }
}
