using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gb_o_tron.mappers
{
    class MBC3 : Mapper
    {
        byte romBank;
        byte ramBank;
        bool readOnly;
        public MBC3(GBCore gb)
        {
            this.gb = gb;
        }
        public override void Power()
        {
            gb.memory.Swap16kROM(0x0000, 0);
            gb.memory.Swap16kROM(0x4000, 1 % (gb.rom.romSize / 16));
            if (gb.rom.RAM)
            {
                gb.memory.Swap8kRAM(0xA000, 0);
                gb.memory.SetReadOnly(0xA000, 8, true);
            }
        }
        public override void Write(byte value, ushort address)
        {
            if (address < 0x2000)
            {
                readOnly = ((value & 0xF) != 0xA);
                gb.memory.SetReadOnly(0xA000, 8, readOnly);
            }
            else if (address < 0x4000)
            {
                value &= 0x7F;
                if (value == 0)
                    value = 1;
                romBank = value;
                gb.memory.Swap16kROM(0x4000, romBank % (gb.rom.romSize / 16));
            }
            else if (address < 0x6000)
            {
                if(value <= 0x3)
                {
                    ramBank = value;
                    gb.memory.Swap8kRAM(0xA000, ramBank % (gb.rom.ramSize / 8));
                    gb.memory.SetReadOnly(0xA000,  8, readOnly);
                }
            }
            else if (address < 0x8000) //RTC latch
            {
            }
        }
    }
}
