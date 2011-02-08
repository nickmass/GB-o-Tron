using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace gb_o_tron
{
    public class LCD
    {
        GBCore gb;
        public byte scanline;
        public int scanlineCycle;
        public byte mode;
        public uint[,] screen = new uint [256,256];
        public int[,] screenZero = new int[256, 256];

        private uint[] bgPal;
        private uint[] windPal;
        private uint[] obp0Pal;
        private uint[] obp1Pal;

        private int[] Flip = { 14, 10, 6, 2, -2, -6, -10, -14 };

        uint alpha = 0x3F000000;
        

        public LCD(GBCore gb)
        {
            this.gb = gb;
            bgPal = new uint[]{ alpha | 0x00FFFFFF, alpha | 0x00AAAAAA, alpha | 0x00555555, alpha | 0x00000000 };
            windPal = new uint[] { alpha | 0x00FFFFFF, alpha | 0x00AAAAAA, alpha | 0x00555555, alpha | 0x00000000 };
            obp0Pal = new uint[] { alpha | 0x00FFFFFF, alpha | 0x00AAAAAA, alpha | 0x00555555, alpha | 0x00000000 };
            obp1Pal = new uint[] { alpha | 0x00FFFFFF, alpha | 0x00AAAAAA, alpha | 0x00555555, alpha | 0x00000000 };

            //Poke red pal?
            /*
            bgPal = new uint[] { alpha | 0x00FFFFFF, alpha | 0x00FE8584, alpha | 0x00943A3B, alpha | 0x00000000 };
            windPal = new uint[] { alpha | 0x00FFFFFF, alpha | 0x00FE8584, alpha | 0x00943A3B, alpha | 0x00000000 };
            obp0Pal = new uint[] { alpha | 0x00FFFFFF, alpha | 0x007CFF31, alpha | 0x00008301, alpha | 0x00000000 };
            obp1Pal = new uint[] { alpha | 0x00FFFFFF, alpha | 0x0065A49A, alpha | 0x000000FE, alpha | 0x00000000 };
            */
            //bgPal = new int[] { -65536, -5636096, -11206656, -13434880 };
            //windPal = new int[] { -16711936, -16733696, -16755456, -16764160 };
            //spritePal = new int[] { -16776961, -16777046, -16777131, -16777165 };
        }
        
        public void DrawScanline()
        {
            if ((gb.LCDC & 0x80) != 0)//LCDEnable
            {
                bool tileTable = (gb.LCDC & 0x10) != 0;
                if ((gb.LCDC & 0x01) != 0) //BGEnable
                {
                    uint[] cgbBGPal;
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
                        uint palColor;
                        int wrap;

                        int begin = horzFlip ? start + 7 : start;
                        int end = horzFlip ? start - 1 : start + 8;
                        int direction = horzFlip ? -1 : 1;

                        bool priority = (attr & 0x80) != 0;

                        cgbBGPal = CGBBGColor(attr & 0x7);

                        for (int xPos = begin; xPos != end; xPos += direction)
                        {
                            wrap = xPos & 0xFF;
                            if (wrap < 160)
                            {
                                color = ((lowChr & 0x80) | (highChr & 0x100)) >> 7;
                                palColor = cgbBGPal[color];
                                screenZero[scanline, wrap] = (color == 0) ? 0 : (priority ? 3 : 2);
                                screen[scanline, wrap] = palColor;
                            }
                            lowChr <<= 1;
                            highChr <<= 1;
                        }
                    }
                }
                if ((gb.LCDC & 0x20) != 0) //WindowEnable
                {
                    uint[] cgbWindowPal;
                    int windowTileMap;
                    if ((gb.LCDC & 0x40) != 0)
                        windowTileMap = 0x9C00;
                    else
                        windowTileMap = 0x9800;
                    int yScroll = (scanline - gb.WY);
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
                                uint palColor;

                                cgbWindowPal = CGBWindColor(attr & 0x7);

                                int begin = horzFlip ? start + 7 : start;
                                int end = horzFlip ? start - 1 : start + 8;
                                int direction = horzFlip ? -1 : 1;

                                bool priority = (attr & 0x80) != 0;

                                for (int xPos = begin; xPos != end ; xPos += direction)
                                {
                                    if (xPos >= 0 && xPos < 160)
                                    {
                                        color = ((lowChr & 0x80) | (highChr & 0x100)) >> 7;
                                        palColor = cgbWindowPal[color];
                                        screenZero[scanline, xPos] = (color == 0) ? (screenZero[scanline, xPos] == 3 ? 3 : 0) : (priority ? 3 : 2);
                                        screen[scanline, xPos] = palColor;
                                    }
                                    lowChr <<= 1;
                                    highChr <<= 1;
                                }
                            }
                        }
                    }
                }
                if ((gb.LCDC & 0x02) != 0) //SpriteEnable
                {
                    bool tallSprites = (gb.LCDC & 0x04) != 0;
                    uint[] cgbSpritePal;
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
                            
                            int lowChr =  gb.memory.ReadVRAM((attr & 0x8) != 0 ? 1 : 0, chrAddr);
                            int highChr =  gb.memory.ReadVRAM((attr & 0x8) != 0 ? 1 : 0, chrAddr + 1) << 1;

                            int begin = horzFlip ? xPos + 7 : xPos;
                            int end = horzFlip ? xPos - 1 : xPos + 8;
                            int direction = horzFlip ? -1 : 1;

                            cgbSpritePal = CGBOBColor(palTable);

                            int color;
                            uint palColor;

                            for (int xPosition = begin; xPosition != end; xPosition += direction)//each pixel in tile
                            {
                                if (xPosition >= 0 && xPosition < 160)
                                {
                                    color = ((lowChr & 0x80) | (highChr & 0x100)) >> 7;
                                    palColor = cgbSpritePal[color];
                                    if (((above && screenZero[scanline, xPosition] == 2) || screenZero[scanline, xPosition] == 0) && color != 0)
                                    {
                                        screen[scanline, xPosition] = palColor;
                                    }
                                }
                                lowChr <<= 1;
                                highChr <<= 1;
                            }
                        }
                    }
                }
            }
        }
        private uint[] CGBWindColor(int index)
        {
            uint[] colors = new uint[4];
            if (gb.rom.cgbMode)
            {
                for (int i = 0; i < 4; i++)
                {
                    int entry = gb.cgbBGP[index * 8 + (i * 2)] | gb.cgbBGP[(index * 8) + 1 + (i * 2)] << 8;
                    colors[i] = alpha | gbcToRgb32(entry);
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    colors[i] = windPal[gb.BGP[i]];
            }
            return colors;
        }
        private uint[] CGBBGColor(int index)
        {
            uint[] colors = new uint[4];
            if (gb.rom.cgbMode)
            {
                for (int i = 0; i < 4; i++)
                {
                    int entry = gb.cgbBGP[index * 8 + (i * 2)] | gb.cgbBGP[(index * 8) + 1 + (i * 2)] << 8;
                    colors[i] = alpha | gbcToRgb32(entry);
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    colors[i] = bgPal[gb.BGP[i]];
            }
            return colors;
        }
        private uint[] CGBOBColor(int index)
        {
            uint[] colors = new uint[4];
            if (gb.rom.cgbMode)
            {
                for (int i = 0; i < 4; i++)
                {
                    int entry = gb.cgbOBP[index * 8 + (i * 2)] | gb.cgbOBP[(index * 8) + 1 + (i * 2)] << 8;
                    colors[i] = alpha | gbcToRgb32(entry);
                }
            }
            else
            {
                if (index == 0)
                {
                    for (int i = 0; i < 4; i++)
                        colors[i] = obp0Pal[gb.OBP0[i]];
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                        colors[i] = obp1Pal[gb.OBP1[i]];
                }
            }
            return colors;
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
                //if (scanline < 144)
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
    }
}
