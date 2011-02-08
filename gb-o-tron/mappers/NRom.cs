using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gb_o_tron.mappers
{
    class NRom : Mapper
    {

        public NRom(GBCore gb)
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
            }
        }
    }
}
