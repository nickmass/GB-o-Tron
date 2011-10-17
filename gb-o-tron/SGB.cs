using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gb_o_tron
{
    public class SGB
    {
        GBCore gb;
        byte[][] commandPackets;
        int streamPos;
        int packetPos;
        int bytePos;
        bool wantStopBit;
        bool inStream;
        byte curJoypad;
        byte totalJoypads = 0xF;
        public int mask;

        public byte[] screenData = new byte[5760];

        public int pendingPalCopy = 0;
        public int pendingAttrCopy = 0;
        public int pendingTileCopy = 0;
        public int pendingBGMapCopy = 0;

        public uint[][] sgbPalettes;
        public uint[] sgbSystemPalettes = new uint[0x800];

        public byte[,] attrTable = new byte[18, 20];
        public byte[] attrSystemTables = new byte[0x1000];

        public byte[] systemTiles = new byte[0x2000];
        public int tileAddr;

        public int[] bgMap = new int[0x1000];
        public uint[,] border = new uint[224, 256];
        public bool newBorder = false;

        public uint[,] screen = new uint[144, 160];

        public SGB(GBCore gb)
        {
            this.gb = gb;
            commandPackets = new byte[7][];
            for (int i = 0; i < 7; i++)
                commandPackets[i] = new byte[16];
            sgbPalettes = new uint[4][];
            for (int i = 0; i < 4; i++)
            {
                sgbPalettes[i] = new uint[16];
                for (int j = 0; j < 16; j++)
                    sgbPalettes[i][j] = sgbToRgb32(0x7FFF);
            }
            for (int i = 0; i < 0x800; i++)
                sgbSystemPalettes[i] = sgbToRgb32(0x7FFF);

        }
        public int Read()
        {
            return curJoypad;
        }
        public void Write(int value)
        {
            int bit = ReadBit(value);
            if (bit == 3)
            {
                streamPos = 0;
                bytePos = 0;
                packetPos = 0;
                wantStopBit = false;
                inStream = true;
                commandPackets = new byte[7][];
                for (int i = 0; i < 7; i++)
                    commandPackets[i] = new byte[16];
            }
            else if ((bit == 0 || bit == 1) && inStream)
            {
                if (wantStopBit) // && bit == 0)
                {
                    if(bit == 0)
                        wantStopBit = false;
                }
                else if (streamPos == 0 || streamPos != (commandPackets[0][0] & 7))
                {
                    commandPackets[streamPos][packetPos] |= (byte)(bit << bytePos);
                    if (streamPos == 0 && packetPos == 0)
                        commandTry += (commandPackets[streamPos][packetPos] >> 3).ToString("X2") + " ";
                    bytePos++;
                    if (bytePos == 8)
                    {
                        bytePos = 0;
                        packetPos++;
                        if (packetPos == 16)
                        {
                            wantStopBit = true;
                            packetPos = 0;
                            streamPos++;
                            if (streamPos >= (commandPackets[0][0] & 7))
                            {
                                if((commandPackets[0][0] & 7) != 0)
                                    ProcessStream();
                                inStream = false;
                            }
                        }
                    }
                }
            }
            else if (bit == 4)
            {
                if (curJoypad == totalJoypads)
                    curJoypad = 0xF;
                else
                    curJoypad--;
            }
        }
        string commandTry = "";
        public void ProcessStream()
        {
            int command = (commandPackets[0][0] >> 3);
            byte[] stream = new byte[((commandPackets[0][0] & 7) * 16) - 1];
            for (int i = 0; i < ((commandPackets[0][0] & 7) * 16) - 1; i++)
            {
                stream[i] = commandPackets[(i + 1) / 16][(i + 1) % 16]; //should cut out length byte
            }
            switch (command)
            {
                case 0x00://SGB Command 00h - PAL01
                    sgbPalettes[0][0] = sgbToRgb32(stream[0] | stream[1] << 8);
                    for (int i = 1; i < 4; i++)
                        sgbPalettes[i][0] = sgbPalettes[0][0];
                    sgbPalettes[0][1] = sgbToRgb32(stream[2] | stream[3] << 8);
                    sgbPalettes[0][2] = sgbToRgb32(stream[4] | stream[5] << 8);
                    sgbPalettes[0][3] = sgbToRgb32(stream[6] | stream[7] << 8);
                    sgbPalettes[1][1] = sgbToRgb32(stream[8] | stream[9] << 8);
                    sgbPalettes[1][2] = sgbToRgb32(stream[10] | stream[11] << 8);
                    sgbPalettes[1][3] = sgbToRgb32(stream[12] | stream[13] << 8);
                    break;
                case 0x01://SGB Command 01h - PAL23
                    sgbPalettes[0][0] = sgbToRgb32(stream[0] | stream[1] << 8);
                    for (int i = 1; i < 4; i++)
                        sgbPalettes[i][0] = sgbPalettes[0][0];
                    sgbPalettes[2][1] = sgbToRgb32(stream[2] | stream[3] << 8);
                    sgbPalettes[2][2] = sgbToRgb32(stream[4] | stream[5] << 8);
                    sgbPalettes[2][3] = sgbToRgb32(stream[6] | stream[7] << 8);
                    sgbPalettes[3][1] = sgbToRgb32(stream[8] | stream[9] << 8);
                    sgbPalettes[3][2] = sgbToRgb32(stream[10] | stream[11] << 8);
                    sgbPalettes[3][3] = sgbToRgb32(stream[12] | stream[13] << 8);
                    break;
                case 0x02://SGB Command 02h - PAL03
                    sgbPalettes[0][0] = sgbToRgb32(stream[0] | stream[1] << 8);
                    for (int i = 1; i < 4; i++)
                        sgbPalettes[i][0] = sgbPalettes[0][0];
                    sgbPalettes[0][1] = sgbToRgb32(stream[2] | stream[3] << 8);
                    sgbPalettes[0][2] = sgbToRgb32(stream[4] | stream[5] << 8);
                    sgbPalettes[0][3] = sgbToRgb32(stream[6] | stream[7] << 8);
                    sgbPalettes[2][1] = sgbToRgb32(stream[8] | stream[9] << 8);
                    sgbPalettes[2][2] = sgbToRgb32(stream[10] | stream[11] << 8);
                    sgbPalettes[2][3] = sgbToRgb32(stream[12] | stream[13] << 8);
                    break;
                case 0x03://SGB Command 03h - PAL12
                    sgbPalettes[0][0] = sgbToRgb32(stream[0] | stream[1] << 8);
                    for (int i = 1; i < 4; i++)
                        sgbPalettes[i][0] = sgbPalettes[0][0];
                    sgbPalettes[0][1] = sgbToRgb32(stream[2] | stream[3] << 8);
                    sgbPalettes[0][2] = sgbToRgb32(stream[4] | stream[5] << 8);
                    sgbPalettes[0][3] = sgbToRgb32(stream[6] | stream[7] << 8);
                    sgbPalettes[3][1] = sgbToRgb32(stream[8] | stream[9] << 8);
                    sgbPalettes[3][2] = sgbToRgb32(stream[10] | stream[11] << 8);
                    sgbPalettes[3][3] = sgbToRgb32(stream[12] | stream[13] << 8);
                    break;
                case 0x04://SGB Command 04h - ATTR_BLK
                    for (int i = 0; i < stream[0]; i++)
                    {
                        int dataSet = (i * 6) + 1;
                        int x1 = stream[dataSet + 2];
                        int y1 = stream[dataSet + 3];
                        int x2 = stream[dataSet + 4];
                        int y2 = stream[dataSet + 5];
                        if ((stream[dataSet] & 1) != 0)
                        {
                            for (int x = x1 + 1; x < x2 && x < 20; x++)
                                for (int y = y1 + 1; y < y2 && y < 18; y++)
                                    attrTable[y, x] = (byte)(stream[dataSet + 1] & 3);
                            if ((stream[dataSet] & 0x6) == 0)//include border if no other colorations are to be made.
                            {
                                for (int x = x1; x <= x2 && x < 20; x++)
                                {
                                    attrTable[y1, x] = (byte)(stream[dataSet + 1] & 3);
                                    attrTable[y2, x] = (byte)(stream[dataSet + 1] & 3);
                                }
                                for (int y = y1; y <= y2 && y < 18; y++)
                                {
                                    attrTable[y, x1] = (byte)(stream[dataSet + 1] & 3);
                                    attrTable[y, x2] = (byte)(stream[dataSet + 1] & 3);
                                }
                            }
                        }
                        if ((stream[dataSet] & 2) != 0)
                        {
                            for (int x = x1; x <= x2 && x < 20; x++)
                            {
                                attrTable[y1, x] = (byte)((stream[dataSet + 1] >> 2) & 3);
                                attrTable[y2, x] = (byte)((stream[dataSet + 1] >> 2) & 3);
                            }
                            for (int y = y1; y <= y2 && y < 18; y++)
                            {
                                attrTable[y, x1] = (byte)((stream[dataSet + 1] >> 2) & 3);
                                attrTable[y, x2] = (byte)((stream[dataSet + 1] >> 2) & 3);
                            }
                        }
                        if ((stream[dataSet] & 4) != 0)
                        {

                            for (int x = 0; x < x1 && x < 20; x++)
                                for (int y = 0; y < 18; y++)
                                    attrTable[y, x] = (byte)((stream[dataSet + 1] >> 4) & 3);
                            for (int x = 0; x < 20; x++)
                                for (int y = 0; y < y1 && y < 18; y++)
                                    attrTable[y, x] = (byte)((stream[dataSet + 1] >> 4) & 3);
                            for (int x = x2 + 1; x < 20; x++)
                                for (int y = 0; y < 18; y++)
                                    attrTable[y, x] = (byte)((stream[dataSet + 1] >> 4) & 3);
                            for (int x = 0; x < 20; x++)
                                for (int y = y2 + 1; y < 18; y++)
                                    attrTable[y, x] = (byte)((stream[dataSet + 1] >> 4) & 3);

                            if ((stream[dataSet] & 0x3) == 0)//include border if no other colorations are to be made.
                            {
                                for (int x = x1; x <= x2 && x < 20; x++)
                                {
                                    attrTable[y1, x] = (byte)((stream[dataSet + 1] >> 4) & 3);
                                    attrTable[y2, x] = (byte)((stream[dataSet + 1] >> 4) & 3);
                                }
                                for (int y = y1; y <= y2 && y < 18; y++)
                                {
                                    attrTable[y, x1] = (byte)((stream[dataSet + 1] >> 4) & 3);
                                    attrTable[y, x2] = (byte)((stream[dataSet + 1] >> 4) & 3);
                                }
                            }

                        }
                    }
                    break;
                case 0x05://SGB Command 05h - ATTR_LIN
                    for (int i = 1; i < stream[0] + 1; i++)
                    {
                        if ((stream[i] & 0x80) != 0)//Horz.
                        {
                            if((stream[i] & 0x1F) < 18)
                                for (int x = 0; x < 20; x++)
                                    attrTable[stream[i] & 0x1F, x] = (byte)((stream[i] >> 5) & 3);
                        }
                        else//Vert.
                        {
                            if ((stream[i] & 0x1F) < 20)
                                for (int y = 0; y < 18; y++)
                                    attrTable[y, stream[i] & 0x1F] = (byte)((stream[i] >> 5) & 3);
                        }
                    }
                    break;
                case 0x06://SGB Command 06h - ATTR_DIV
                    if ((stream[0] & 0x40) != 0)//Horz
                    {
                        for (int x = 0; x < 20; x++)
                            for (int y = 0; y < stream[1] && y < 18; y++)
                                attrTable[y, x] = (byte)((stream[0] >> 2) & 3);
                        if(stream[1] < 18)
                        {
                            for (int x = 0; x < 20; x++)
                            {
                                attrTable[stream[1], x] = (byte)((stream[0] >> 4) & 3);
                            }
                            for (int x = 0; x < 20; x++)
                                for (int y = stream[1] + 1; y < 18; y++)
                                    attrTable[y, x] = (byte)((stream[0]) & 3);
                        }

                    }
                    else//Vert
                    {
                        for (int x = 0; x < stream[1] && x < 20; x++)
                            for (int y = 0; y < 18; y++)
                                attrTable[y, x] = (byte)((stream[0] >> 2) & 3);
                        if (stream[1] < 20)
                        {
                            for (int y = 0; y < 18; y++)
                            {
                                attrTable[y, stream[1]] = (byte)((stream[0] >> 4) & 3);
                            }
                            for (int x = stream[1] + 1; x < 20; x++)
                                for (int y = 0; y < 18; y++)
                                    attrTable[y, x] = (byte)((stream[0]) & 3);
                        }
                    }
                    break;
                case 0x07://SGB Command 07h - ATTR_CHR
                    int dataSets = stream[2] | (stream[3] << 8);
                    int xStart = stream[0];
                    int yStart = stream[1];
                    if ((stream[4] & 1) != 0)//Left to Right
                    {
                        for (int i = 0; i < dataSets; i++)
                        {
                            int shift = 0;
                            if ((i % 4) == 0)
                                shift = 6;
                            else if ((i % 4) == 1)
                                shift = 4;
                            else if ((i % 4) == 2)
                                shift = 2;
                            attrTable[yStart, xStart] = (byte)((stream[(i / 4) + 5] >> shift) & 0x3);
                            xStart++;
                            if (xStart >= 20)
                            {
                                yStart++;
                                xStart = 0;
                                if (yStart >= 18)
                                    yStart = 0;
                            }
                        }
                    }
                    else//Top to Bottom
                    {
                        for (int i = 0; i < dataSets; i++)
                        {
                            int shift = 0;
                            if ((i % 4) == 0)
                                shift = 6;
                            else if ((i % 4) == 1)
                                shift = 4;
                            else if ((i % 4) == 2)
                                shift = 2;
                            attrTable[yStart, xStart] = (byte)((stream[(i / 4) + 5] >> shift) & 0x3);
                            yStart++;
                            if (yStart >= 18)
                            {
                                xStart++;
                                yStart = 0;
                                if (xStart >= 20)
                                    xStart = 0;
                            }
                        }
                    }
                    break;
                case 0x0A://SGB Command 0Ah - PAL_SET
                    for (int i = 0; i < 4; i++)
                    {
                        int palette = (stream[(i * 2)] | (stream[(i * 2) + 1] << 8)) & 0x1FF;
                        for (int j = 0; j < 4; j++)
                            sgbPalettes[i][j] = sgbSystemPalettes[(palette * 4) + j];
                    }
                    for (int i = 0; i < 4; i++)
                        sgbPalettes[i][0] = sgbPalettes[0][0];//has to be set to palette 0,0 for kirbys dreamland 2
                    if ((stream[8] & 0x80) != 0)
                    {
                        if ((stream[8] & 0x3F) < 0x2D)
                        {
                            for (int i = 0; i < 90; i++)
                            {
                                attrTable[i / 5, ((i % 5) * 4) + 0] = (byte)((attrSystemTables[((stream[8] & 0x3F) * 90) + i] >> 6) & 3);
                                attrTable[i / 5, ((i % 5) * 4) + 1] = (byte)((attrSystemTables[((stream[8] & 0x3F) * 90) + i] >> 4) & 3);
                                attrTable[i / 5, ((i % 5) * 4) + 2] = (byte)((attrSystemTables[((stream[8] & 0x3F) * 90) + i] >> 2) & 3);
                                attrTable[i / 5, ((i % 5) * 4) + 3] = (byte)((attrSystemTables[((stream[8] & 0x3F) * 90) + i] >> 0) & 3);
                            }
                        }
                    }
                    if ((stream[8] & 0x40) != 0)
                        mask = 0;
                    break;
                case 0x0B://SGB Command 0Bh - PAL_TRN
                    pendingPalCopy = 1;
                    break;
                case 0x11://SGB Command 11h - MLT_REQ
                    if (stream[0] == 0 || stream[0] == 2)
                        totalJoypads = 0x0F;
                    else if (stream[0] == 1)
                        totalJoypads = 0x0E;
                    else if (stream[0] == 3)
                        totalJoypads = 0x0C;
                    break;
                case 0x13://SGB Command 13h - CHR_TRN
                    pendingTileCopy = 1;
                    tileAddr = (stream[0] & 0x1) != 0 ? 0x1000 : 0x0000;
                    break;
                case 0x14://SGB Command 14h - PCT_TRN
                    pendingBGMapCopy = 1;
                    break;
                case 0x15://SGB Command 15h - ATTR_TRN
                    pendingAttrCopy = 1;
                    break;
                case 0x16://SGB Command 16h - ATTR_SET
                    if ((stream[0] & 0x3F) < 0x2D)
                    {
                        for(int i = 0; i < 90; i++)
                        {
                            attrTable[i / 5, ((i % 5) * 4) + 0] = (byte)((attrSystemTables[((stream[0] & 0x3F) * 90) + i] >> 6) & 3);
                            attrTable[i / 5, ((i % 5) * 4) + 1] = (byte)((attrSystemTables[((stream[0] & 0x3F) * 90) + i] >> 4) & 3);
                            attrTable[i / 5, ((i % 5) * 4) + 2] = (byte)((attrSystemTables[((stream[0] & 0x3F) * 90) + i] >> 2) & 3);
                            attrTable[i / 5, ((i % 5) * 4) + 3] = (byte)((attrSystemTables[((stream[0] & 0x3F) * 90) + i] >> 0) & 3);
                        }
                    }
                    if ((stream[0] & 0x40) != 0)
                        mask = 0;
                    break;
                case 0x17://SGB Command 17h - MASK_EN
                    mask = stream[0] & 3;
                    break;
            }
            commandQ += command.ToString("X2") + " ";
        }
        string commandQ;
        static uint sgbToRgb32(int bgr15)
        {
            uint r = (uint)(bgr15 & 0x1F);
            uint g = (uint)(bgr15 >> 5 & 0x1F);
            uint b = (uint)(bgr15 >> 10 & 0x1F);
            
            return ((r * 13 + g * 2 + b) >> 1) << 16 | ((g * 3 + b) << 9) | ((r * 3 + g * 2 + b * 11) >> 1) | 0xFF000000;
        }
        public static int ReadBit(int value)
        {
            if ((value & 0x10) == 0 && (value & 0x20) == 0)
                return 3;//Reset
            if ((value & 0x10) == 0)
                return 0;//0
            if ((value & 0x20) == 0)
                return 1;//1
            return 4;//Ignore
        }
        public void Frame()
        {
            ApplySGB();
            if (pendingPalCopy >= 1)
                pendingPalCopy++;
            if (pendingAttrCopy >= 1)
                pendingAttrCopy++;
            if (pendingTileCopy >= 1)
                pendingTileCopy++;
            if (pendingBGMapCopy >= 1)
                pendingBGMapCopy++;
            if (pendingPalCopy >= 4)
            {
                pendingPalCopy = 0;
                for (int i = 0; i < 0x800; i++)
                {
                    sgbSystemPalettes[i] = sgbToRgb32(screenData[(i * 2)] | (screenData[((i * 2) + 1)] << 8));
                    screenData[i * 2] = 0;
                    screenData[(i * 2) + 1] = 0;
                }
            }
            if (pendingAttrCopy >= 4)
            {
                pendingAttrCopy = 0;
                for (int i = 0; i < 0x1000; i++)
                {
                    attrSystemTables[i] = screenData[i];
                    screenData[i] = 0;
                }
            }
            if (pendingTileCopy >= 4)
            {
                pendingTileCopy = 0;
                for (int i = 0; i < 0x1000; i++)
                {
                    systemTiles[i | tileAddr] = screenData[i];
                    screenData[i] = 0;
                }
                DrawBorder();
            }
            if (pendingBGMapCopy >= 4)
            {
                pendingBGMapCopy = 0;
                for (int i = 0; i < 0x800; i++)
                {
                    bgMap[i] = screenData[(i * 2)] | (screenData[((i * 2) + 1)] << 8);
                    screenData[i * 2] = 0;
                    screenData[(i * 2) + 1] = 0;
                }
                DrawBorder();
            }
        }
        public void ApplySGB()
        {
            for (int scanline = 0; scanline < 144; scanline++) //this might mess some stuff up or it may be more accurate, from a certain prespective it does make more sense for the gameboy to be handled a frame at a time.
            {
                for (int x = 0; x < 160; x++)
                {
                    if (pendingAttrCopy == 3 || pendingPalCopy == 3 || pendingTileCopy == 3 || pendingBGMapCopy == 3)
                    {
                        int tileNumber = ((scanline / 8) * 20) + (x / 8);
                        int xOff = 7 - (x % 8);
                        int yOff = (scanline % 8);
                        int tileAddr = tileNumber * 16;
                        tileAddr += yOff * 2;
                        uint high = (gb.lcd.screen[scanline, x] >> 1) & 1;
                        uint low = gb.lcd.screen[scanline, x] & 1;
                        screenData[tileAddr] |= (byte)(low << xOff);
                        screenData[tileAddr + 1] |= (byte)(high << xOff);
                    }
                    if (mask == 0)//No Mask
                        screen[scanline, x] = sgbPalettes[attrTable[scanline / 8, x / 8]][gb.lcd.screen[scanline, x]]; //Made the decision to not apply alpha to SGB games
                    else if (gb.sgb.mask == 2)//Back Mask
                        screen[scanline, x] = 0xFF000000;
                    else if (gb.sgb.mask == 3)//Color 0 Mask
                        screen[scanline, x] = sgbPalettes[0][0];
                    //Else freeze on last frame
                }
            }
        }
        public void DrawBorder()
        {
            newBorder = true;
            uint[][] palettes = new uint[4][];
            for (int i = 0; i < 4; i++)
            {
                palettes[i] = new uint[16];
                for(int j = 0; j < 16; j++)
                {
                    palettes[i][j] = sgbToRgb32(bgMap[0x400 + (16 * i) + (j)]);
                }
            }

            for (int tileX = 0; tileX < 32; tileX++)
            {
                for (int tileY = 0; tileY < 28; tileY++)
                {
                    int tileAddr = (tileY * 32) + tileX;
                    int tile = bgMap[tileAddr];
                    int palette = ((tile >> 10) & 7) - 4;
                    if (palette >= 0)
                    {
                        int chrAddr = (tile & 0xFF) * 32;
                        bool horzFlip = (tile & 0x4000) != 0;
                        bool vertFlip = (tile & 0x8000) != 0;
                        int xStart = horzFlip ? 0 : 7;
                        int xEnd = horzFlip ? 8 : -1;
                        int xDirection = horzFlip ? 1 : -1;
                        for (int line = 0; line < 8; line++)
                        {
                            int aChr = systemTiles[chrAddr];
                            int bChr = systemTiles[chrAddr + 1] << 1;
                            int cChr = systemTiles[chrAddr + 16] << 2;
                            int dChr = systemTiles[chrAddr + 16 + 1] << 3;
                            int vertFlipper = vertFlip ? 7 - line : line;
                            for (int x = xStart; x != xEnd; x += xDirection)
                            {
                                int color = (aChr & 1) | (bChr & 2) | (cChr & 4) | (dChr & 8);
                                if (color == 0 && ((tileX >= 6 && tileX < 26) && (tileY >= 5 && tileY < 23))) //Super Snakey border overlap uses color 0 as transparent on title screen
                                    border[(tileY * 8) + line, (tileX * 8) + x] = 0x00000000;
                                else
                                {
                                    if (color == 0)
                                        border[(tileY * 8) + vertFlipper, (tileX * 8) + x] = sgbToRgb32(0xFFFF) & 0xFFFFFFFF;
                                    else
                                        border[(tileY * 8) + vertFlipper, (tileX * 8) + x] = palettes[palette][color] & 0xFFFFFFFF;
                                }
                                aChr >>= 1;
                                bChr >>= 1;
                                cChr >>= 1;
                                dChr >>= 1;
                            }
                            chrAddr += 2;
                        }
                    }
                    else
                    {
                        uint color;
                        if (!((tileX >= 6 && tileX < 26) && (tileY >= 5 && tileY < 23)))
                            color = sgbToRgb32(0xFFFF) & 0xFFFFFFFF;
                        else
                            color = 0x00000000;
                        for (int line = 0; line < 8; line++)
                        {
                            for (int x = 0; x < 8; x++)
                            {

                                border[(tileY * 8) + line, (tileX * 8) + x] = color;
                            }
                        }
                    }
                }
            }
        }
    }
}
