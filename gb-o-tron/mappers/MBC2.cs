using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace gb_o_tron.mappers
{
    class MBC2 : Mapper
    {
        public MBC2(GBCore gb)
        {
            this.gb = gb;
        }
        public override void Power()
        {
            gb.memory.Swap16kROM(0x0000, 0);
            gb.memory.Swap16kROM(0x4000, 1 % (gb.rom.romSize / 16));
            gb.memory.Swap1kRAM(0xA000, 0);
            //gb.memory.SetReadOnly(0xA000, 1, true);
        }
        public override void Write(byte value, ushort address)
        {
            if (address < 0x2000)
            {
                if ((address & 0x100) == 0)
                {
                    gb.memory.SetReadOnly(0xA000, 1, ((value & 0xF) != 0xA));
                }
            }
            else if (address < 0x4000)
            {
                if ((address & 0x100) != 0)
                {
                    value &= 0x0F;
                    if (value == 0)
                        value = 1;
                    gb.memory.Swap16kROM(0x4000, value % (gb.rom.romSize / 16));
                }
            }
        }
        public override byte Read(byte value, ushort address)
        {
            if (address >= 0xA000 && address < 0xA200)
                return (byte)(value & 0xF);
            else
                return value;
        }
    }
}
