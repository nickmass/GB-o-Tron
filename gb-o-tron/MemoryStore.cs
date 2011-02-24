using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace gb_o_tron
{
    public class MemoryStore
    {
        public byte[][] banks;
        public int[] memMap = new int[0x40];
        bool[] readOnly = new bool[0x40];
        public bool[] saveBanks;
        public int swapOffset;
        public int ramSwapOffset;
        public int vramSwapOffset;
        public int wramSwapOffset;
        public MemoryStore(int banks, bool readOnly)
        {
            this.banks = new byte[banks][];
            saveBanks = new bool[banks];
            for (int i = 0; i < banks; i++)
            {
                this.banks[i] = new byte[0x400];
                saveBanks[i] = false;
            }
            for (int i = 0; i < 0x40; i++)
            {
                this.readOnly[i] = readOnly;
                memMap[i] = i;
            }
        }
        public byte this[int address]
        {
            get
            {
                return banks[memMap[address >> 0xA]][address & 0x3FF];
            }
            set
            {
                if (!readOnly[address >> 0xA])
                {
                    saveBanks[memMap[address >> 0xA]] = true;
                    banks[memMap[address >> 0xA]][address & 0x3FF] = value;
                }
            }
        }
        public void ForceValue(int address, byte value)
        {
            banks[memMap[address >> 0xA]][address & 0x3FF] = value;
        }
        public void SetReadOnly(ushort address, int kb, bool readOnly)
        {
            for (int i = 0; i < kb; i++)
                this.readOnly[(address >> 0xA) + i] = readOnly;
        }
        public void SwapWRAM(int bank)
        {
            if (bank == 0)
                bank = 1;
            memMap[0x34] = (bank * 4) + wramSwapOffset;
            memMap[0x35] = (bank * 4) + 1 + wramSwapOffset;
            memMap[0x36] = (bank * 4) + 2 + wramSwapOffset;
            memMap[0x37] = (bank * 4) + 3 + wramSwapOffset;

            readOnly[0x34] = false;
            readOnly[0x35] = false;
            readOnly[0x36] = false;
            readOnly[0x37] = false;
        }
        public void SwapVRAM(int bank)
        {
            memMap[0x20] = (bank * 8) + 0 + vramSwapOffset;
            memMap[0x21] = (bank * 8) + 1 + vramSwapOffset;
            memMap[0x22] = (bank * 8) + 2 + vramSwapOffset;
            memMap[0x23] = (bank * 8) + 3 + vramSwapOffset;
            memMap[0x24] = (bank * 8) + 4 + vramSwapOffset;
            memMap[0x25] = (bank * 8) + 5 + vramSwapOffset;
            memMap[0x26] = (bank * 8) + 6 + vramSwapOffset;
            memMap[0x27] = (bank * 8) + 7 + vramSwapOffset;
            readOnly[0x20] = false;
            readOnly[0x21] = false;
            readOnly[0x22] = false;
            readOnly[0x23] = false;
            readOnly[0x24] = false;
            readOnly[0x25] = false;
            readOnly[0x26] = false;
            readOnly[0x27] = false;
        }
        public byte ReadVRAM(int bank, int address)
        {
            address -= 0x8000;
            return banks[(bank * 8) + (address >> 0xA) + vramSwapOffset][address & 0x03FF];
        }
        public void Swap1kROM(ushort address, int bank)
        {
            bank += swapOffset;
            memMap[address >> 0xA] = bank;
            readOnly[address >> 0xA] = true;
        }
        public void Swap2kROM(ushort address, int bank)
        {
            bank *= 2;
            Swap1kROM(address, bank);
            Swap1kROM((ushort)(address + 0x400), bank + 1);
        }
        public void Swap4kROM(ushort address, int bank)
        {
            bank *= 2;
            Swap2kROM(address, bank);
            Swap2kROM((ushort)(address + 0x800), bank + 1);
        }
        public void Swap8kROM(ushort address, int bank)
        {
            bank *= 2;
            Swap4kROM(address, bank);
            Swap4kROM((ushort)(address + 0x1000), bank + 1);
        }
        public void Swap16kROM(ushort address, int bank)
        {
            bank *= 2;
            Swap8kROM(address, bank);
            Swap8kROM((ushort)(address + 0x2000), bank + 1);
        }
        public void Swap32kROM(ushort address, int bank)
        {
            bank *= 2;
            Swap16kROM(address, bank);
            Swap16kROM((ushort)(address + 0x4000), bank + 1);
        }
        public void Swap1kRAM(ushort address, int bank)
        {
            bank += ramSwapOffset;
            memMap[address >> 0xA] = bank;
            readOnly[address >> 0xA] = false;
        }
        public void Swap2kRAM(ushort address, int bank)
        {
            bank *= 2;
            Swap1kRAM(address, bank);
            Swap1kRAM((ushort)(address + 0x400), bank + 1);
        }
        public void Swap4kRAM(ushort address, int bank)
        {
            bank *= 2;
            Swap2kRAM(address, bank);
            Swap2kRAM((ushort)(address + 0x800), bank + 1);
        }
        public void Swap8kRAM(ushort address, int bank)
        {
            bank *= 2;
            Swap4kRAM(address, bank);
            Swap4kRAM((ushort)(address + 0x1000), bank + 1);
        }
        public void Swap16kRAM(ushort address, int bank)
        {
            bank *= 2;
            Swap8kRAM(address, bank);
            Swap8kRAM((ushort)(address + 0x2000), bank + 1);
        }
        public void Swap32kRAM(ushort address, int bank)
        {
            bank *= 2;
            Swap16kRAM(address, bank);
            Swap16kRAM((ushort)(address + 0x4000), bank + 1);
        }
        public void StateSave(BinaryWriter writer)
        {
            writer.Write(memMap.Length);
            for (int i = 0; i < memMap.Length; i++)
                writer.Write(memMap[i]);
            for (int i = 0; i < readOnly.Length; i++)
                writer.Write(readOnly[i]);
            int changedBanks = 0;
            for (int i = 0; i < saveBanks.Length; i++)
                if (saveBanks[i])
                    changedBanks++;
            writer.Write(changedBanks);
            for (int i = 0; i < saveBanks.Length; i++)
            {
                if (saveBanks[i])
                {
                    writer.Write("BANK");
                    writer.Write(i);
                    for (int j = 0; j < 0x400; j++)
                    {
                        writer.Write(banks[i][j]);
                    }
                }
            }
        }

        public void StateLoad(BinaryReader reader)
        {
            int memLength = reader.ReadInt32();
            for (int i = 0; i < memLength; i++)
                memMap[i] = reader.ReadInt32();
            for (int i = 0; i < readOnly.Length; i++)
                readOnly[i] = reader.ReadBoolean();
            for (int i = 0; i < saveBanks.Length; i++)
                saveBanks[i] = false;
            int saveLength = reader.ReadInt32();
            for (int i = 0; i < saveLength; i++)
            {
                string bbb = reader.ReadString();
                int bankNumber = reader.ReadInt32();
                saveBanks[bankNumber] = true;
                for (int j = 0; j < 0x400; j++)
                {
                    banks[bankNumber][j] = reader.ReadByte();
                }
            }
        }
    }
}
