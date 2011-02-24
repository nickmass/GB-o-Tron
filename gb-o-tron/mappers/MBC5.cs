using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace gb_o_tron.mappers
{
    class MBC5 : Mapper
    {
        byte romBank;
        byte romBankHigh;
        byte ramBank;
        bool readOnly;
        public MBC5(GBCore gb)
        {
            this.gb = gb;
        }
        public override void Power()
        {
            gb.memory.Swap16kROM(0x0000, 0);
            gb.memory.Swap16kROM(0x4000, 0);
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
            else if (address < 0x3000)
            {
                if (value == 0)
                    value = 1;
                romBank = value;
                gb.memory.Swap16kROM(0x4000, (romBank | (romBankHigh << 8)) % (gb.rom.romSize / 16));
            }
            else if (address < 0x4000)
            {
                romBankHigh = (byte)(value & 1);
                gb.memory.Swap16kROM(0x4000, (romBank | (romBankHigh << 8)) % (gb.rom.romSize / 16));
            }
            else if (address < 0x6000)
            {
                if (gb.rom.RAM)
                {
                    ramBank = (byte)(value & 0xF);
                    gb.memory.Swap8kRAM(0xA000, ramBank % (gb.rom.ramSize / 8));
                    gb.memory.SetReadOnly(0xA000, 8, readOnly);
                }
            }
        }
        public void StateSave(BinaryWriter writer)
        {
            writer.Write(romBank);
            writer.Write(romBankHigh);
            writer.Write(ramBank);
            writer.Write(readOnly);
        }
        public void StateLoad(BinaryReader reader)
        {
            romBank = reader.ReadByte();
            romBankHigh = reader.ReadByte();
            ramBank = reader.ReadByte();
            readOnly = reader.ReadBoolean();
        }
    }
}
