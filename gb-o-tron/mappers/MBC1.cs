using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace gb_o_tron.mappers
{
    class MBC1 : Mapper
    {
        bool ramMode;
        byte romBank;
        byte ramBank;
        bool readOnly;
        public MBC1(GBCore gb)
        {
            this.gb = gb;
        }
        public override void Power()
        {
            gb.memory.Swap16kROM(0x0000, 0);
            gb.memory.Swap16kROM(0x4000, 1 % (gb.rom.romSize / 16));
            if (gb.rom.RAM)
            {
                if (gb.rom.ramSize == 2)
                {
                    gb.memory.Swap2kRAM(0xA000, 0);
                    gb.memory.Swap2kRAM(0xA800, 0);
                    gb.memory.Swap2kRAM(0xB000, 0);
                    gb.memory.Swap2kRAM(0xB800, 0);
                }
                else
                    gb.memory.Swap8kRAM(0xA000, 0);
                gb.memory.SetReadOnly(0xA000, 8, true);
            }
            else
            {
                gb.memory.memMap[0x28] = 0;
                gb.memory.memMap[0x29] = 0;
                gb.memory.memMap[0x2A] = 0;
                gb.memory.memMap[0x2B] = 0;
                gb.memory.memMap[0x2C] = 0;
                gb.memory.memMap[0x2D] = 0;
                gb.memory.memMap[0x2E] = 0;
                gb.memory.memMap[0x2F] = 0;
                gb.memory.SetReadOnly(0xA000, 8, true);
            }
        }
        public override void Write(byte value, ushort address)
        {
            if (address < 0x2000)
            {
                readOnly = ((value & 0xF) != 0xA) || !gb.rom.RAM;
                gb.memory.SetReadOnly(0xA000, 8, readOnly);
            }
            else if (address < 0x4000)
            {
                value &= 0x1F;
                if (value == 0)
                    value = 1;
                romBank = value;
                if (ramMode)
                    gb.memory.Swap16kROM(0x4000, romBank % (gb.rom.romSize / 16));
                else
                    gb.memory.Swap16kROM(0x4000, (romBank | (ramBank << 5)) % (gb.rom.romSize / 16));
            }
            else if (address < 0x6000)
            {
                value &= 3;
                ramBank = value;
                SyncRom();
            }
            else if (address < 0x8000)
            {
                ramMode = ((value & 0x1) != 0);
                SyncRom();
            }
        }
        private void SyncRom()
        {
            if (ramMode)
            {
                if (gb.rom.RAM)
                {
                    if (gb.rom.ramSize == 2)
                    {
                        gb.memory.Swap2kRAM(0xA000, 0);
                        gb.memory.Swap2kRAM(0xA800, 0);
                        gb.memory.Swap2kRAM(0xB000, 0);
                        gb.memory.Swap2kRAM(0xB800, 0);
                    }
                    else
                        gb.memory.Swap8kRAM(0xA000, ramBank % (gb.rom.ramSize / 8));
                    gb.memory.SetReadOnly(0xA000, 8, readOnly);
                }
                gb.memory.Swap16kROM(0x4000, romBank % (gb.rom.romSize / 16));
            }
            else
            {

                if (gb.rom.RAM)
                {
                    if (gb.rom.ramSize == 2)
                    {
                        gb.memory.Swap2kRAM(0xA000, 0);
                        gb.memory.Swap2kRAM(0xA800, 0);
                        gb.memory.Swap2kRAM(0xB000, 0);
                        gb.memory.Swap2kRAM(0xB800, 0);
                    }
                    else
                        gb.memory.Swap8kRAM(0xA000, 0);
                    gb.memory.SetReadOnly(0xA000, (gb.rom.ramSize == 2) ? 2 : 8, readOnly);
                }
                gb.memory.Swap16kROM(0x4000, (romBank | (ramBank << 5)) % (gb.rom.romSize / 16));
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(ramMode);
            writer.Write(romBank);
            writer.Write(ramBank);
            writer.Write(readOnly);
        }
        public override void StateLoad(BinaryReader reader)
        {
            ramMode = reader.ReadBoolean();
            romBank = reader.ReadByte();
            ramBank = reader.ReadByte();
            readOnly = reader.ReadBoolean();
        }
    }
}
