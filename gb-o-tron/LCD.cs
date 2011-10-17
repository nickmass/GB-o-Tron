using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace gb_o_tron
{
    public class LCD
    {
        GBCore gb;
        public byte scanline;
        public int scanlineCycle;
        public byte mode = 2;
        public uint[,] screen = new uint [144,160];
        public int[,] screenZero = new int[144, 160];

        private uint[][] bgPal;
        private uint[][] obpPal;

        private int[] Flip = { 14, 10, 6, 2, -2, -6, -10, -14 };
        public uint alpha = 0xFF000000;

        public uint[] grayBGP;
        public uint[] grayOBP0;
        public uint[] grayOBP1;

        public LCD(GBCore gb)
        {
            this.gb = gb;
            bgPal = new uint[8][];
            obpPal = new uint[8][];
            for (int i = 0; i < 8; i++)
            {
                bgPal[i] = new uint[4];
                obpPal[i] = new uint[4];
            }
            grayBGP = new uint[] { alpha | 0x00FFFFFF, alpha | 0x00AAAAAA, alpha | 0x00555555, alpha | 0x00000000 };
            grayOBP0 = new uint[] { alpha | 0x00FFFFFF, alpha | 0x00AAAAAA, alpha | 0x00555555, alpha | 0x00000000 };
            grayOBP1 = new uint[] { alpha | 0x00FFFFFF, alpha | 0x00AAAAAA, alpha | 0x00555555, alpha | 0x00000000 };
        }
        
        public void DrawScanline()
        {
            if ((gb.LCDC & 0x80) != 0)//LCDEnable
            {
                bool tileTable = (gb.LCDC & 0x10) != 0;
                if ((gb.LCDC & 0x01) != 0) //BGEnable
                {
                    int bgTileMap;
                    if ((gb.LCDC & 0x08) != 0)
                        bgTileMap = 0x9C00;
                    else
                        bgTileMap = 0x9800;
                    int yScroll = scanline + gb.SCY;
                    int coarseYScroll = yScroll & 0xF8;
                    int fineYScroll = yScroll & 0x07;
                    for (int x = 0; x < 32; x++)
                    {
                        int chrAddr;
                        byte tileNumber = gb.memory.ReadVRAM(0, bgTileMap | (coarseYScroll << 2) | x);
                        byte attr = gb.rom.cgbMode ? gb.memory.ReadVRAM(1, bgTileMap | (coarseYScroll << 2) | x) : (byte)0;

                        bool horzFlip = (attr & 0x20) != 0;
                        bool vertFlip = (attr & 0x40) != 0;

                        if (tileTable)
                            chrAddr = (tileNumber << 4) + 0x8000;
                        else
                            chrAddr = (((sbyte)tileNumber) << 4) + 0x9000;

                        chrAddr += fineYScroll << 1;
                        chrAddr += (vertFlip ? Flip[fineYScroll] : 0);

                        int lowChr = gb.memory.ReadVRAM((attr & 0x8) != 0 ? 1 : 0, chrAddr);
                        int highChr = gb.memory.ReadVRAM((attr & 0x8) != 0 ? 1 : 0, chrAddr + 1) << 1;

                        int start = ((x * 8) - gb.SCX);

                        int color;
                        int wrap;

                        int begin = horzFlip ? start : start + 7;
                        int end = horzFlip ? start + 8 : start - 1;
                        int direction = horzFlip ? 1 : -1;

                        bool priority = (attr & 0x80) != 0;

                        int palTable = attr & 0x7;

                        for (int xPos = begin; xPos != end; xPos += direction)
                        {
                            wrap = xPos & 0xFF;
                            if (wrap < 160)
                            {
                                color = (lowChr & 0x01) | (highChr & 0x02);
                                screenZero[scanline, wrap] = (color == 0) ? 0 : (priority ? 3 : 2);
                                screen[scanline, wrap] = bgPal[palTable][color];
                            }
                            lowChr >>= 1;
                            highChr >>= 1;
                        }
                    }
                }
                else
                {
                    for (int x = 0; x < 160; x++)
                    {
                        screen[scanline, x] = bgPal[0][0];
                    }
                }
                if ((gb.LCDC & 0x20) != 0) //WindowEnable
                {
                    int windowTileMap;
                    if ((gb.LCDC & 0x40) != 0)
                        windowTileMap = 0x9C00;
                    else
                        windowTileMap = 0x9800;
                    int yScroll = scanline - gb.WY;
                    int xScroll = gb.WX - 7;
                    if (yScroll < 144 && yScroll >= 0 && xScroll < 160)
                    {
                        int coarseYScroll = yScroll & 0xF8;
                        int fineYScroll = yScroll & 0x07;
                        for (int x = 0; x < 32; x++)
                        {
                            int start = ((x * 8) + xScroll);
                            if (start < 160)
                            {
                                int chrAddr;

                                byte tileNumber = gb.memory.ReadVRAM(0, windowTileMap | (coarseYScroll << 2) | x);
                                byte attr = gb.rom.cgbMode ? gb.memory.ReadVRAM(1, windowTileMap | (coarseYScroll << 2) | x) : (byte)0;

                                bool horzFlip = (attr & 0x20) != 0;
                                bool vertFlip = (attr & 0x40) != 0;

                                if (tileTable)
                                    chrAddr = (tileNumber << 4) + 0x8000;
                                else
                                    chrAddr = (((sbyte)tileNumber) << 4) + 0x9000;

                                chrAddr += fineYScroll << 1;
                                chrAddr += (vertFlip ? Flip[fineYScroll] : 0);

                                int lowChr = gb.memory.ReadVRAM((attr & 0x8) != 0 ? 1 : 0, chrAddr);
                                int highChr = gb.memory.ReadVRAM((attr & 0x8) != 0 ? 1 : 0, chrAddr + 1) << 1;

                                int color;

                                int palTable = attr & 0x7;

                                int begin = horzFlip ? start : start + 7;
                                int end = horzFlip ? start + 8 : start - 1;
                                int direction = horzFlip ? 1 : -1;

                                bool priority = (attr & 0x80) != 0;

                                for (int xPos = begin; xPos != end; xPos += direction)
                                {
                                    if (xPos >= 0 && xPos < 160)
                                    {
                                        color = (lowChr & 0x1) | (highChr & 0x2);
                                        screenZero[scanline, xPos] = (color == 0) ? (screenZero[scanline, xPos] == 3 ? 3 : 0) : (priority ? 3 : 2);
                                        screen[scanline, xPos] = bgPal[palTable][color];
                                    }
                                    lowChr >>= 1;
                                    highChr >>= 1;
                                }
                            }
                        }
                    }
                }
                if ((gb.LCDC & 0x02) != 0) //SpriteEnable
                {
                    bool tallSprites = (gb.LCDC & 0x04) != 0;
                    for (int sprite = 39; sprite >= 0; sprite--)
                    {
                        int yPos = gb.oamRam[(sprite << 2)] - 16;
                        if (yPos <= scanline && yPos + (tallSprites ? 16 : 8) > scanline)
                        {
                            int xPos = gb.oamRam[(sprite << 2) | 1] - 8;
                            int spriteY = (scanline - yPos);
                            int tileNumber = gb.oamRam[(sprite << 2) | 2];
                            int attr = gb.oamRam[(sprite << 2) | 3];
                            bool horzFlip = (attr & 0x20) != 0;
                            bool vertFlip = (attr & 0x40) != 0;
                            int palTable = gb.rom.cgbMode ? (attr & 7) : ((attr & 0x10) >> 4);
                            bool above = (attr & 0x80) == 0;
                            if (tallSprites)
                            {
                                tileNumber &= 0xFE;
                                if (spriteY > 7)
                                    tileNumber |= 1;
                            }
                            int chrAddr = ((tileNumber << 4) | 0x8000 | ((spriteY & 7) * 2)) + (vertFlip ? tallSprites ? (spriteY > 7) ? Flip[spriteY & 7] - (1 << 4) : Flip[spriteY & 7] + (1 << 4) : Flip[spriteY & 7] : 0);

                            int lowChr = gb.memory.ReadVRAM((attr & 0x8) != 0 ? 1 : 0, chrAddr);
                            int highChr = gb.memory.ReadVRAM((attr & 0x8) != 0 ? 1 : 0, chrAddr + 1) << 1;

                            int begin = horzFlip ? xPos : xPos + 7;
                            int end = horzFlip ? xPos + 8 : xPos - 1;
                            int direction = horzFlip ? 1 : -1;

                            int color;

                            for (int xPosition = begin; xPosition != end; xPosition += direction)//each pixel in tile
                            {
                                if (xPosition >= 0 && xPosition < 160)
                                {
                                    color = ((lowChr & 0x1) | (highChr & 0x2));
                                    if (((above && screenZero[scanline, xPosition] == 2) || screenZero[scanline, xPosition] == 0) && color != 0)
                                    {
                                        screen[scanline, xPosition] = obpPal[palTable][color];
                                    }
                                }
                                lowChr >>= 1;
                                highChr >>= 1;
                            }
                        }
                    }
                }
            }
            else
            {
                for (int x = 0; x < 160; x++)
                {
                    screen[scanline, x] = bgPal[0][0];
                }
            }
        }
        public void CopyCGBColors()
        {
            for (int col = 0; col < 4; col++)
            {
                grayBGP[col] = bgPal[0][col];
                grayOBP0[col] = obpPal[0][col];
                grayOBP1[col] = obpPal[1][col];
            }
        }
        public void UpdatePalette(bool cgb)
        {
            if (cgb)
            {
                for (int pal = 0; pal < 8; pal++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        bgPal[pal][col] = gbcToRgb32(gb.cgbBGP[pal * 8 + (col * 2)] | gb.cgbBGP[(pal * 8) + 1 + (col * 2)] << 8) | alpha;
                        obpPal[pal][col] = gbcToRgb32(gb.cgbOBP[pal * 8 + (col * 2)] | gb.cgbOBP[(pal * 8) + 1 + (col * 2)] << 8) | alpha;
                    }
                }
            }
            else if(!gb.rom.cgbMode)
            {
                if (gb.rom.sgbMode)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        bgPal[0][col] = (byte)gb.BGP[col];
                        obpPal[0][col] = (byte)gb.OBP0[col];
                        obpPal[1][col] = (byte)gb.OBP1[col];
                    }
                }
                else
                {
                    for (int col = 0; col < 4; col++)
                    {
                        bgPal[0][col] = grayBGP[gb.BGP[col]];
                        obpPal[0][col] = grayOBP0[gb.OBP0[col]];
                        obpPal[1][col] = grayOBP1[gb.OBP1[col]];
                    }
                }
            }
        }
        static uint gbcToRgb32(int bgr15) {
	        uint r = (uint)(bgr15 & 0x1F);
	        uint g = (uint)(bgr15 >> 5 & 0x1F);
            uint b = (uint)(bgr15 >> 10 & 0x1F);

	        return ((r * 13 + g * 2 + b) >> 1) << 16 | ((g * 3 + b) << 9) | ((r * 3 + g * 2 + b * 11) >> 1);
        }
        int cycleCount = 0;
        public void AddCycles(int cycles)
        {
            scanlineCycle += cycles;
            cycleCount += cycles;
            if (scanlineCycle >= 456)
            {
                gb.InterSTATHBlank = false;
                scanline++;
                gb.LY++;
                if (gb.LY >= 154)
                    gb.LY = 0;
                scanlineCycle -= 456;
                if (scanline >= 144 && mode != 1)//vblank
                {
                    gb.InterVBlank = true;
                    gb.InterSTATVBlank = true;
                    mode = 1;
                }
                else if (scanline >= 154)//end vblank
                {
                    cycleCount = 0;
                    gb.emulating = false;
                    mode = 2;
                    //gb.InterVBlank = false;
                    gb.InterSTATVBlank = false;
                    gb.InterSTATOAM = true;
                    scanline = 0;
                    if (gb.rom.sgbMode)
                        gb.sgb.Frame();
                }
                else if(mode != 1)
                {
                    mode = 2;
                    gb.InterSTATOAM = true;
                }
            } // 204, 80, 172
            else if (scanlineCycle >= 252 && scanline < 144 && mode != 0)
            {
                DrawScanline();
                mode = 0;
                gb.InterSTATHBlank = true;
                if (gb.DMAActive && gb.DMAHBlank)
                {
                    int i =0;
                    while (i < 0x10 && gb.DMAPosition < gb.DMALength)
                    {
                        gb.memory[gb.DMADstAddress + gb.DMAPosition] = gb.memory[gb.DMASourceAddress + gb.DMAPosition];
                        gb.DMAPosition++;
                        i++;
                    }
                    if (gb.DMAPosition == gb.DMALength)
                    {
                        gb.DMAActive = false;
                    }
                    gb.AddCycles(8);
                }
            }
            else if (scanlineCycle >= 80 && scanlineCycle < 252 && scanline < 144)
            {
                gb.InterSTATOAM = false;
                mode = 3;
            }
            if (gb.LY == gb.LYC)
                gb.InterSTATCoincidence = true;
            else
                gb.InterSTATCoincidence = false;
        }
        public void StateSave(BinaryWriter writer)
        {
            writer.Write(scanline);
            writer.Write(scanlineCycle);
            writer.Write(mode);
        }
        public void StateLoad(BinaryReader reader)
        {
            scanline = reader.ReadByte();
            scanlineCycle = reader.ReadInt32();
            mode = reader.ReadByte();
            UpdatePalette(gb.rom.cgbMode);
        }
    }
}
