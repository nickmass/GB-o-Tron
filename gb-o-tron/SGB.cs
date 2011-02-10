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
        bool mask;

        public byte[] screenData = new byte[5760];

        public int pendingPalCopy = 0;
        public int pendingAttrCopy = 0;

        public uint[][] sgbPalettes;
        public uint[] sgbSystemPalettes = new uint[0x800];

        public byte[,] attrTable = new byte[18, 20];
        public byte[] attrSystemTables = new byte[0x1000];

        public SGB(GBCore gb)
        {
            this.gb = gb;
            commandPackets = new byte[7][];
            for (int i = 0; i < 7; i++)
                commandPackets[i] = new byte[16];
            sgbPalettes = new uint[8][];
            for (int i = 0; i < 8; i++)
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
            else if (bit == 0)
            {
                if (curJoypad == totalJoypads)
                    curJoypad = 0xF;
                else
                    curJoypad--;
            }
        }
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
                    for (int i = 1; i < 8; i++)
                        sgbPalettes[i][0] = sgbPalettes[0][0];
                    for (int i = 0; i < 6; i++)
                        sgbPalettes[i / 3][(i % 3) + 1] = sgbToRgb32(stream[(i * 2) + 2] | stream[(i * 2) + 3] << 8);
                    break;
                case 0x01://SGB Command 01h - PAL23
                    sgbPalettes[0][0] = sgbToRgb32(stream[0] | stream[1] << 8);
                    for (int i = 1; i < 8; i++)
                        sgbPalettes[i][0] = sgbPalettes[0][0];
                    for (int i = 0; i < 6; i++)
                        sgbPalettes[(i / 3) + 2][(i % 3) + 1] = sgbToRgb32(stream[(i * 2) + 2] | stream[(i * 2) + 3] << 8);
                    break;
                case 0x02://SGB Command 02h - PAL03
                    sgbPalettes[0][0] = sgbToRgb32(stream[0] | stream[1] << 8);
                    for (int i = 1; i < 8; i++)
                        sgbPalettes[i][0] = sgbPalettes[0][0];
                    for (int i = 0; i < 6; i++)
                        sgbPalettes[(i / 3) * 3][(i % 3) + 1] = sgbToRgb32(stream[(i * 2) + 2] | stream[(i * 2) + 3] << 8);
                    break;
                case 0x03://SGB Command 03h - PAL12
                    sgbPalettes[0][0] = sgbToRgb32(stream[0] | stream[1] << 8);
                    for (int i = 1; i < 8; i++)
                        sgbPalettes[i][0] = sgbPalettes[0][0];
                    for (int i = 0; i < 6; i++)
                        sgbPalettes[(i / 3) + 1][(i % 3) + 1] = sgbToRgb32(stream[(i * 2) + 2] | stream[(i * 2) + 3] << 8);
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
                            //sgbPalettes[i][j] = sgbSystemPalettes[palette][j];
                    }
                    for (int i = 1; i < 8; i++)
                        sgbPalettes[i][0] = sgbPalettes[3][0];
                    if ((stream[8] & 0x80) != 0)
                    {
                        if ((stream[8] & 0x3F) < 0x2D)
                        {
                            for (int i = 0; i < 90; i++)
                            {
                                attrTable[i / 5, ((i % 5) * 4) + 0] = (byte)((attrSystemTables[((stream[8] & 0x3F) * 90) + (i % 90)] >> 6) & 3);
                                attrTable[i / 5, ((i % 5) * 4) + 1] = (byte)((attrSystemTables[((stream[8] & 0x3F) * 90) + (i % 90)] >> 4) & 3);
                                attrTable[i / 5, ((i % 5) * 4) + 2] = (byte)((attrSystemTables[((stream[8] & 0x3F) * 90) + (i % 90)] >> 2) & 3);
                                attrTable[i / 5, ((i % 5) * 4) + 3] = (byte)((attrSystemTables[((stream[8] & 0x3F) * 90) + (i % 90)] >> 0) & 3);
                            }
                        }
                    }
                    if ((stream[8] & 0x40) != 0)
                        mask = false;
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
                case 0x15://SGB Command 15h - ATTR_TRN
                    pendingAttrCopy = 1;
                    break;
                case 0x16://SGB Command 16h - ATTR_SET
                    if ((stream[0] & 0x3F) < 0x2D)
                    {
                        for(int i = 0; i < 90; i++)
                        {
                            attrTable[i / 5, ((i % 5) * 4) + 0] = (byte)((attrSystemTables[((stream[0] & 0x3F) * 90) + (i % 90)] >> 6) & 3);
                            attrTable[i / 5, ((i % 5) * 4) + 1] = (byte)((attrSystemTables[((stream[0] & 0x3F) * 90) + (i % 90)] >> 4) & 3);
                            attrTable[i / 5, ((i % 5) * 4) + 2] = (byte)((attrSystemTables[((stream[0] & 0x3F) * 90) + (i % 90)] >> 2) & 3);
                            attrTable[i / 5, ((i % 5) * 4) + 3] = (byte)((attrSystemTables[((stream[0] & 0x3F) * 90) + (i % 90)] >> 0) & 3);
                        }
                    }
                    if ((stream[0] & 0x40) != 0)
                        mask = false;
                    break;
                case 0x17://SGB Command 17h - MASK_EN
                    if (stream[0] == 0)
                        mask = false;
                    else
                        mask = true; //Should support the different masking types but screen freezing would be a pain
                    break;
            }
        }
        static uint sgbToRgb32(int bgr15)
        {
            uint r = (uint)(bgr15 & 0x1F);
            uint g = (uint)(bgr15 >> 5 & 0x1F);
            uint b = (uint)(bgr15 >> 10 & 0x1F);
            
            return ((r * 13 + g * 2 + b) >> 1) << 16 | ((g * 3 + b) << 9) | ((r * 3 + g * 2 + b * 11) >> 1);
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
            if (pendingPalCopy >= 1)
                pendingPalCopy++;
            if (pendingAttrCopy >= 1)
                pendingAttrCopy++;
            if (pendingPalCopy >= 4)
            {
                pendingPalCopy = 0;
                for (int i = 0; i < 0x800; i++)
                    sgbSystemPalettes[i] = sgbToRgb32(screenData[(i * 2)] | (screenData[((i * 2) + 1)] << 8));
            }
            if (pendingAttrCopy >= 4)
            {
                pendingAttrCopy = 0;
                for (int i = 0; i < 0x1000; i++)
                {
                    attrSystemTables[i] = screenData[i];
                }
            }
        }
    }
}
