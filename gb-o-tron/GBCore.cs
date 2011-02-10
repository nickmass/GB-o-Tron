using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using gb_o_tron.mappers;

namespace gb_o_tron
{
    public struct Input
    {
        public bool up;
        public bool down;
        public bool left;
        public bool right;
        public bool a;
        public bool b;
        public bool select;
        public bool start;
    }
    public struct Rom
    {
        public string title;
        public bool cgbMode;
        public string newLicense;
        public bool SGB;
        public byte cartType;
        public bool RAM;
        public bool battery;
        public bool timer;
        public bool rumble;
        public int romSize;
        public int ramSize;
        public byte dst;
        public byte oldLicense;
        public byte maskRomVer;
        public byte headerChecksum;
        public ushort glocalChecksum;
    }
    public class GBCore
    {
        private Input input;
        public Rom rom;

        private int regA;
        private int regB;
        private int regC;
        private int regD;
        private int regE;
        private int regH;
        private int regL;
        private int regSP;
        private int regPC;
        private int intZ;
        private bool flagZ
        {
            get
            {
                return intZ == 0;
            }
            set
            {
                if (value)
                    intZ = 0;
                else
                    intZ = 1;
            }
        }
        private bool flagN;
        private bool flagH;
        private bool flagC;
        private int regF
        {
            get
            {
                byte value = 0;
                if (flagZ)
                    value |= 0x80;
                if (flagN)
                    value |= 0x40;
                if (flagH)
                    value |= 0x20;
                if (flagC)
                    value |= 0x10;
                return value;
            }
            set
            {
                flagZ = ((value & 0x80) != 0);
                flagN = ((value & 0x40) != 0);
                flagH = ((value & 0x20) != 0);
                flagC = ((value & 0x10) != 0);
            }
        }
        private int regAF
        {
            get
            {
                return ((regA << 8) | regF) & 0xFFFF;
            }
            set
            {
                regA = (value >> 8) & 0xFF;
                regF = (value & 0xFF);
            }
        }
        private int regBC
        {
            get
            {
                return ((regB << 8) | regC) & 0xFFFF;
            }
            set
            {
                regB = (value >> 8) & 0xFF;
                regC = (value & 0xFF);
            }
        }
        private int regDE
        {
            get
            {
                return ((regD << 8) | regE) & 0xFFFF;
            }
            set
            {
                regD = (value >> 8) & 0xFF;
                regE = (value & 0xFF);
            }
        }
        private int regHL
        {
            get
            {
                return ((regH << 8) | regL) & 0xFFFF;
            }
            set
            {
                regH = (value >> 8) & 0xFF;
                regL = (value & 0xFF);
            }
        }
        private bool IME;

        public bool InterVBlank;
        public bool InterTimer;
        public bool InterSerial;
        public bool InterJoypad;

        public bool InterVBlankEnabled;
        public bool InterTimerEnabled;
        public bool InterSerialEnabled;
        public bool InterJoypadEnabled;
        public bool InterLCDSTATEnabled;

        private bool selectDirections;
        private bool selectButtons;

        public bool InterLCDSTAT
        {
            get
            {
                return (InterSTATOAM && InterSTATOAMEnabled) || (InterSTATHBlank && InterSTATHBlankEnabled) || (InterSTATVBlank && InterSTATVBlankEnabled) || (InterSTATCoincidence && InterSTATCoincidenceEnabled) || InterSTATRequest;
            }
            set
            {
                if (value)
                    InterSTATRequest = true;
                else
                {
                    InterSTATOAM = false;
                    InterSTATHBlank = false;
                    InterSTATVBlank = false;
                    InterSTATCoincidence = false;
                    InterSTATRequest = false;
                }
            }
        }
        public bool InterSTATOAM;
        public bool InterSTATHBlank;
        public bool InterSTATVBlank;
        public bool InterSTATCoincidence;
        public bool InterSTATRequest;

        public bool InterSTATOAMEnabled;
        public bool InterSTATHBlankEnabled;
        public bool InterSTATVBlankEnabled;
        public bool InterSTATCoincidenceEnabled;

        private bool timerEnabled;
        private byte timerReload;
        private byte timer;
        private int timerClock;
        private byte timerClockMode;
        private int timerClocker;

        private byte divider;
        private int dividerClocker;

        private bool halted;
        public bool emulating;
        public int cycles;

        public MemoryStore memory;
        public mappers.Mapper mapper;
        public LCD lcd;
        public SGB sgb;

        byte serialByte;
        public string serialData;

        public int cpuClock;

        public int CGBSPEED = 8388608;
        public int NORMALSPEED = 4194304;

        public byte LY;
        public byte LYC;
        public byte SCY;
        public byte SCX;
        public byte WY;
        public byte WX;

        int opCycles = 0;

        public int[] BGP = new int[4];
        public int[] OBP0 = new int[4];
        public int[] OBP1 = new int[4];

        public byte[] oamRam = new byte[0xA0];
        public byte[] hRam = new byte[0x100];

        public byte LCDC;

        public byte[] cgbBGP = new byte[0x40];
        public byte[] cgbOBP = new byte[0x40];

        public int cgbBGPIndex;
        bool cgbBGPAuto;

        public int cgbOBPIndex;
        bool cgbOBPAuto;

        int wramBank;

        bool pendingSpeedChange;

        public int DMASourceAddress;
        public int DMADstAddress;
        public int DMALength;
        public int DMAPosition;
        public bool DMAActive;
        public bool DMAHBlank;

        bool forceClassicGB = false;

        byte[][] bios = new byte[3][];
        byte[][] bootBanks = new byte[3][];
        bool booted = false;

        public GBCore(Stream image)
        {
            image.Position = 0x134;
            cpuClock = NORMALSPEED;
            for(int i = 0; i < 15; i++)
            {
                char titleChar = (char)image.ReadByte();
                if(titleChar != '\0')
                    rom.title += titleChar;
            }
            rom.cgbMode = (image.ReadByte() & 0x80) != 0 && !forceClassicGB;
            rom.newLicense = ((char)image.ReadByte()).ToString() + ((char)image.ReadByte()).ToString();
            rom.SGB = image.ReadByte() == 0x03 && !forceClassicGB;
            rom.cartType = (byte)image.ReadByte();
            int romSize = image.ReadByte();
            switch(romSize)
            {
                case 0x52:
                    rom.romSize = 1152;
                    break;
                case 0x53:
                    rom.romSize = 1280;
                    break;
                case 0x54:
                    rom.romSize = 1536;
                    break;
                default:
                    rom.romSize = 32 << romSize;
                    break;
            }
            int ramSize = image.ReadByte();
            switch(ramSize)
            {
                case 0x00:
                    rom.RAM = false;
                    rom.ramSize = 0;
                    break;
                case 0x01:
                    rom.RAM = true;
                    rom.ramSize = 2;
                    break;
                case 0x02:
                    rom.RAM = true;
                    rom.ramSize = 8;
                    break;
                case 0x03:
                    rom.RAM = true;
                    rom.ramSize = 32;
                    break;
            }
            rom.dst = (byte)image.ReadByte();
            rom.oldLicense = (byte)image.ReadByte();
            rom.SGB = (rom.SGB && rom.oldLicense == 0x33);
            rom.maskRomVer = (byte)image.ReadByte();
            rom.headerChecksum = (byte)image.ReadByte();
            rom.glocalChecksum = (ushort)(image.ReadByte() | (image.ReadByte() << 8));
            memory = new MemoryStore(48 + rom.romSize + rom.ramSize, true);
            memory.swapOffset = 48;
            memory.vramSwapOffset = 0;
            memory.wramSwapOffset = 16;
            memory.ramSwapOffset = memory.swapOffset + rom.romSize;
            memory.SwapVRAM(0);//VRAM
            memory.memMap[0x30] = 16;//WRAM0
            memory.memMap[0x31] = 17;
            memory.memMap[0x32] = 18;
            memory.memMap[0x33] = 19;
            memory.SetReadOnly(0xC000, 4, false);
            wramBank = 1;
            memory.SwapWRAM(wramBank);//WRAM1
            image.Position = 0;
            for (int i = 0x00; i < rom.romSize * 0x400; i++)
                memory.banks[(i / 0x400) + memory.swapOffset][i % 0x400] = (byte)image.ReadByte();
            image.Close();
            switch (rom.cartType)
            {
                case 0x09:
                    rom.battery = true;
                    goto case 0x00;
                case 0x08:
                case 0x00: //NROM
                    mapper = new NRom(this);
                    break;
                case 0x03:
                    rom.battery = true;
                    goto case 0x01;
                case 0x02:
                case 0x01: //MBC1
                    mapper = new MBC1(this);
                    break;
                case 0x0F:
                case 0x10:
                    rom.timer = true;
                    rom.battery = true;
                    goto case 0x11;
                case 0x13:
                    rom.battery = true;
                    goto case 0x11;
                case 0x12:
                case 0x11://MBC3
                    mapper = new MBC3(this);
                    break;
                case 0x1E:
                    rom.rumble = true;
                    rom.battery = true;
                    goto case 0x19;
                case 0x1B:
                    rom.battery = true;
                    goto case 0x19;
                case 0x1C:
                case 0x1D:
                    rom.rumble = true;
                    goto case 0x19;
                case 0x19://MBC5
                    mapper = new MBC5(this);
                    break;
                default:
                    MessageBox.Show(rom.cartType.ToString());
                    goto case 0x01;
                    
            }
            lcd = new LCD(this);
            if (rom.SGB)
                sgb = new SGB(this);
            if (rom.cgbMode)
                LoadBios();
            else
                Power();
        }
        public void LoadBios()
        {
            mapper.Power();
            System.Reflection.Assembly thisExe;
            thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            Stream biosStream = thisExe.GetManifestResourceStream("gb_o_tron.gbc_bios.bin");
            bios[0] = new byte[0x400];
            bios[1] = new byte[0x400];
            bios[2] = new byte[0x400];
            bootBanks[0] = new byte[0x400];
            bootBanks[1] = new byte[0x400];
            bootBanks[2] = new byte[0x400];
            for (int i = 0x00; i < 0x900; i++)
                bios[(i / 0x400)][i % 0x400] = (byte)biosStream.ReadByte();
            for (int i = 0x00; i < 0x100; i++)
                bios[0][0x100 + i] = memory.banks[memory.swapOffset][0x100 + i];
            for (int i = 0x00; i < 0xC00; i++)
                bootBanks[(i / 0x400)][i % 0x400] = memory.banks[memory.swapOffset + (i / 0x400)][i % 0x400];
            memory.banks[memory.swapOffset] = bios[0];
            memory.banks[memory.swapOffset + 1] = bios[1];
            memory.banks[memory.swapOffset + 2] = bios[2];
        }
        public void Boot()
        {
            for (int i = 0x00; i < memory.wramSwapOffset * 0x400; i++)
            {
                memory.banks[(i / 0x400)][i % 0x400] = 0; //pokemon yellow work around
            }
            memory.banks[memory.swapOffset] = bootBanks[0];
            memory.banks[memory.swapOffset + 1] = bootBanks[1];
            memory.banks[memory.swapOffset + 2] = bootBanks[2];
            booted = true;
        }
        public void Power()
        {
            mapper.Power();
            booted = true;
            IME = false;
            regPC = 0x0100;
            regAF = 0x01B0;
            if (rom.cgbMode)
                regA = 0x11;
            regBC = 0x0013;
            regDE = 0x00D8;
            regHL = 0x014D;
            regSP = 0xFFFE;
            Write(00, 0xFF0F); //IF
            Write(00, 0xFFFF); //IE
        }
        public void Run(Input player)
        {
            if ((player.a && !input.a) || (player.b && !input.b) || (player.up && !input.up) || (player.down && !input.down) || (player.left && !input.left) || (player.right && !input.right) || (player.select && !input.select) || (player.start && !input.start))
            {
                InterJoypad = true;
            }
            input = player;
            byte opCode =0;
            int tmp;
            int otherTmp;
            emulating = true;
            while (emulating)
            {
                #region cpu
                if (!halted)
                {
                    //if (regPC == 0xC000)
                    //    cycles = -1000;
                    //if(cycles < 0)
                    //    serialData += regPC.ToString("X4") + " ";
                    if (!booted && regPC == 0x100)
                    {
                        Boot();
                    }
                    opCode = Read();
                    opCycles = 0;
                    switch (opCode)
                    {
                        case 0x00: //NOP
                            opCycles += 4;
                            break;
                        case 0x01: //LD BC,nn
                            regBC = ReadWord();
                            opCycles += 12;
                            break;
                        case 0x02: //LD (BC),A
                            Write(regA, regBC);
                            opCycles += 8;
                            break;
                        case 0x03: //INC BC
                            regBC++;
                            regBC &= 0xFFFF;
                            opCycles += 8;
                            break;
                        case 0x04: //INC B
                            regB = INC(regB);
                            opCycles += 4;
                            break;
                        case 0x05: //DEC B
                            regB = DEC(regB);
                            opCycles += 4;
                            break;
                        case 0x06: //LD B,n
                            regB = Read();
                            opCycles += 8;
                            break;
                        case 0x07: //RLC A
                            regA = RLC(regA);
                            flagZ = false;
                            opCycles += 4;
                            break;
                        case 0x08: //LD (nn),SP
                            WriteWord(regSP, ReadWord());
                            opCycles += 16;
                            break;
                        case 0x09: //ADD HL,BC
                            regHL = ADD16(regHL, regBC);
                            opCycles += 8;
                            break;
                        case 0x0A: //LD A,(BC)
                            regA = Read(regBC);
                            opCycles += 8;
                            break;
                        case 0x0B: //DEC BC
                            regBC--;
                            regBC &= 0xFFFF;
                            opCycles += 8;
                            break;
                        case 0x0C: //INC C
                            regC = INC(regC);
                            opCycles += 4;
                            break;
                        case 0x0D: //DEC C
                            regC = DEC(regC);
                            opCycles += 4;
                            break;
                        case 0x0E: //LD C,n
                            regC = Read();
                            opCycles += 8;
                            break;
                        case 0x0F: //RRC A
                            regA = RRC(regA);
                            flagZ = false;
                            opCycles += 4;
                            break;
                        case 0x10: //STOP
                            Read();
                            halted = true;
                            if (pendingSpeedChange)
                            {
                                if (cpuClock == NORMALSPEED)
                                    cpuClock = CGBSPEED;
                                else
                                    cpuClock = NORMALSPEED;
                                pendingSpeedChange = false;
                            }
                            opCycles += 4;
                            break;
                        case 0x11: //LD DE,nn
                            regDE = ReadWord();
                            opCycles += 12;
                            break;
                        case 0x12: //LD (DE),A
                            Write(regA, regDE);
                            opCycles += 8;
                            break;
                        case 0x13: //INC DE
                            regDE++;
                            regDE &= 0xFFFF;
                            opCycles += 8;
                            break;
                        case 0x14: //INC D
                            regD = INC(regD);
                            opCycles += 4;
                            break;
                        case 0x15: //DEC D
                            regD = DEC(regD);
                            opCycles += 4;
                            break;
                        case 0x16: //LD D,n
                            regD = Read();
                            opCycles += 8;
                            break;
                        case 0x17: //RL A
                            regA = RL(regA);
                            flagZ = false;
                            opCycles += 4;
                            break;
                        case 0x18: //JR n
                            regPC = JR(Read(), regPC);
                            opCycles += 12;
                            break;
                        case 0x19: //ADD HL,DE
                            regHL = ADD16(regHL, regDE);
                            opCycles += 8;
                            break;
                        case 0x1A: //LD A,(DE)
                            regA = Read(regDE);
                            opCycles += 8;
                            break;
                        case 0x1B: //DEC DE
                            regDE--;
                            regDE &= 0xFFFF;
                            opCycles += 8;
                            break;
                        case 0x1C: //INC E
                            regE = INC(regE);
                            opCycles += 4;
                            break;
                        case 0x1D: //DEC E
                            regE = DEC(regE);
                            opCycles += 4;
                            break;
                        case 0x1E: //LD E,n
                            regE = Read();
                            opCycles += 8;
                            break;
                        case 0x1F: //RR A
                            regA = RR(regA);
                            flagZ = false;
                            opCycles += 4;
                            break;
                        case 0x20: //JR NZ,n
                            tmp = Read();
                            if (!flagZ)
                            {
                                regPC = JR(tmp, regPC);
                                opCycles += 4;
                            }
                            opCycles += 12;
                            break;
                        case 0x21: //LD HL,nn
                            regHL = ReadWord();
                            opCycles += 12;
                            break;
                        case 0x22: //LDI (HL),A
                            Write(regA, regHL);
                            regHL++;
                            regHL &= 0xFFFF;
                            opCycles += 8;
                            break;
                        case 0x23: //INC HL
                            regHL++;
                            regHL &= 0xFFFF;
                            opCycles += 8;
                            break;
                        case 0x24: //INC H
                            regH = INC(regH);
                            opCycles += 4;
                            break;
                        case 0x25: //DEC H
                            regH = DEC(regH);
                            opCycles += 4;
                            break;
                        case 0x26: //LD H,n
                            regH = Read();
                            opCycles += 8;
                            break;
                        case 0x27: //DAA
                            tmp = flagC ? 0x60 : 0x00;
                            if (flagH)
                                tmp |= 0x06;
                            if (!flagN)
                            {
                                if ((regA & 0xF) > 0x9)
                                    tmp |= 0x06;
                                if (regA > 0x99)
                                    tmp |= 0x60;
                                regA += tmp;
                            }
                            else
                            {
                                regA -= tmp;
                            }
                            regA &= 0xFF;
                            intZ = regA;
                            flagC = (tmp & 0x60) != 0;
                            flagH = false;
                            opCycles += 4;
                            break;
                        case 0x28: //JR Z,n
                            tmp = Read();
                            if (flagZ)
                            {
                                regPC = JR(tmp, regPC);
                                opCycles += 4;
                            }
                            opCycles += 12;
                            break;
                        case 0x29: //ADD HL,HL
                            regHL = ADD16(regHL, regHL);
                            opCycles += 8;
                            break;
                        case 0x2A: //LDI A,(HL)
                            regA = Read(regHL);
                            regHL++;
                            regHL &= 0xFFFF;
                            opCycles += 8;
                            break;
                        case 0x2B: //DEC HL
                            regHL--;
                            regHL &= 0xFFFF;
                            opCycles += 8;
                            break;
                        case 0x2C: //INC L
                            regL = INC(regL);
                            opCycles += 4;
                            break;
                        case 0x2D: //DEC L
                            regL = DEC(regL);
                            opCycles += 4;
                            break;
                        case 0x2E: //LD L,n
                            regL = Read();
                            opCycles += 8;
                            break;
                        case 0x2F: //CPL
                            regA = (regA ^ 0xFF) & 0xFF;
                            flagN = true;
                            flagH = true;
                            opCycles += 4;
                            break;
                        case 0x30: //JR NC,n
                            tmp = Read();
                            if (!flagC)
                            {
                                regPC = JR(tmp, regPC);
                                opCycles += 4;
                            }
                            opCycles += 12;
                            break;
                        case 0x31: //LD SP,nn
                            regSP = ReadWord();
                            opCycles += 12;
                            break;
                        case 0x32: //LDD (HL),A
                            Write(regA, regHL);
                            regHL--;
                            regHL &= 0xFFFF;
                            opCycles += 8;
                            break;
                        case 0x33: //INC SP
                            regSP++;
                            regSP &= 0xFFFF;
                            opCycles += 8;
                            break;
                        case 0x34: //INC (HL)
                            Write(INC(Read(regHL)), regHL);
                            opCycles += 12;
                            break;
                        case 0x35: //DEC (HL)
                            Write(DEC(Read(regHL)), regHL);
                            opCycles += 12;
                            break;
                        case 0x36: //LD (HL),n
                            Write(Read(), regHL);
                            opCycles += 12;
                            break;
                        case 0x37: //SCF
                            flagH = false;
                            flagN = false;
                            flagC = true;
                            opCycles += 4;
                            break;
                        case 0x38: //JR C,n
                            tmp = Read();
                            if (flagC)
                            {
                                regPC = JR(tmp, regPC);
                                opCycles += 4;
                            }
                            opCycles += 12;
                            break;
                        case 0x39: //ADD HL,SP
                            regHL = ADD16(regHL, regSP);
                            opCycles += 8;
                            break;
                        case 0x3A: //LDD A,(HL)
                            regA = Read(regHL);
                            regHL--;
                            regHL &= 0xFFFF;
                            opCycles += 8;
                            break;
                        case 0x3B: //DEC SP
                            regSP--;
                            regSP &= 0xFFFF;
                            opCycles += 8;
                            break;
                        case 0x3C: //INC A
                            regA = INC(regA);
                            opCycles += 4;
                            break;
                        case 0x3D: //DEC A
                            regA = DEC(regA);
                            opCycles += 4;
                            break;
                        case 0x3E: //LD A,n
                            regA = Read();
                            opCycles += 8;
                            break;
                        case 0x3F: //CCF
                            flagH = false;
                            flagN = false;
                            flagC = !flagC;
                            opCycles += 4;
                            break;
                        case 0x40: //LD B,B
                            opCycles += 4;
                            break;
                        case 0x41: //LD B,C
                            regB = regC;
                            opCycles += 4;
                            break;
                        case 0x42: //LD B,D
                            regB = regD;
                            opCycles += 4;
                            break;
                        case 0x43: //LD B,E
                            regB = regE;
                            opCycles += 4;
                            break;
                        case 0x44: //LD B,H
                            regB = regH;
                            opCycles += 4;
                            break;
                        case 0x45: //LD B,L
                            regB = regL;
                            opCycles += 4;
                            break;
                        case 0x46: //LD B,(HL)
                            regB = Read(regHL);
                            opCycles += 8;
                            break;
                        case 0x47: //LD B,A
                            regB = regA;
                            opCycles += 4;
                            break;
                        case 0x48: //LD C,B
                            regC = regB;
                            opCycles += 4;
                            break;
                        case 0x49: //LD C,C
                            opCycles += 4;
                            break;
                        case 0x4A: //LD C,D
                            regC = regD;
                            opCycles += 4;
                            break;
                        case 0x4B: //LD C,E
                            regC = regE;
                            opCycles += 4;
                            break;
                        case 0x4C: //LD C,H
                            regC = regH;
                            opCycles += 4;
                            break;
                        case 0x4D: //LD C,L
                            regC = regL;
                            opCycles += 4;
                            break;
                        case 0x4E: //LD C,(HL)
                            regC = Read(regHL);
                            opCycles += 8;
                            break;
                        case 0x4F: //LD C,A
                            regC = regA;
                            opCycles += 4;
                            break;
                        case 0x50: //LD D,B
                            regD = regB;
                            opCycles += 4;
                            break;
                        case 0x51: //LD D,C
                            regD = regC;
                            opCycles += 4;
                            break;
                        case 0x52: //LD D,D
                            opCycles += 4;
                            break;
                        case 0x53: //LD D,E
                            regD = regE;
                            opCycles += 4;
                            break;
                        case 0x54: //LD D,H
                            regD = regH;
                            opCycles += 4;
                            break;
                        case 0x55: //LD D,L
                            regD = regL;
                            opCycles += 4;
                            break;
                        case 0x56: //LD D,(HL)
                            regD = Read(regHL);
                            opCycles += 8;
                            break;
                        case 0x57: //LD D,A
                            regD = regA;
                            opCycles += 4;
                            break;
                        case 0x58: //LD E,B
                            regE = regB;
                            opCycles += 4;
                            break;
                        case 0x59: //LD E,C
                            regE = regC;
                            opCycles += 4;
                            break;
                        case 0x5A: //LD E,D
                            regE = regD;
                            opCycles += 4;
                            break;
                        case 0x5B: //LD E,E
                            opCycles += 4;
                            break;
                        case 0x5C: //LD E,H
                            regE = regH;
                            opCycles += 4;
                            break;
                        case 0x5D: //LD E,L
                            regE = regL;
                            opCycles += 4;
                            break;
                        case 0x5E: //LD E,(HL)
                            regE = Read(regHL);
                            opCycles += 8;
                            break;
                        case 0x5F: //LD E,A
                            regE = regA;
                            opCycles += 4;
                            break;
                        case 0x60: //LD H,B
                            regH = regB;
                            opCycles += 4;
                            break;
                        case 0x61: //LD H,C
                            regH = regC;
                            opCycles += 4;
                            break;
                        case 0x62: //LD H,D
                            regH = regD;
                            opCycles += 4;
                            break;
                        case 0x63: //LD H,E
                            regH = regE;
                            opCycles += 4;
                            break;
                        case 0x64: //LD H,H
                            opCycles += 4;
                            break;
                        case 0x65: //LD H,L
                            regH = regL;
                            opCycles += 4;
                            break;
                        case 0x66: //LD B,(HL)
                            regH = Read(regHL);
                            opCycles += 8;
                            break;
                        case 0x67: //LD H,A
                            regH = regA;
                            opCycles += 4;
                            break;
                        case 0x68: //LD L,B
                            regL = regB;
                            opCycles += 4;
                            break;
                        case 0x69: //LD L,C
                            regL = regC;
                            opCycles += 4;
                            break;
                        case 0x6A: //LD L,D
                            regL = regD;
                            opCycles += 4;
                            break;
                        case 0x6B: //LD L,E
                            regL = regE;
                            opCycles += 4;
                            break;
                        case 0x6C: //LD L,H
                            regL = regH;
                            opCycles += 4;
                            break;
                        case 0x6D: //LD L,L
                            opCycles += 4;
                            break;
                        case 0x6E: //LD L,(HL)
                            regL = Read(regHL);
                            opCycles += 8;
                            break;
                        case 0x6F: //LD L,A
                            regL = regA;
                            opCycles += 4;
                            break;
                        case 0x70: //LD (HL),B
                            Write(regB, regHL);
                            opCycles += 8;
                            break;
                        case 0x71: //LD (HL),C
                            Write(regC, regHL);
                            opCycles += 8;
                            break;
                        case 0x72: //LD (HL),D
                            Write(regD, regHL);
                            opCycles += 8;
                            break;
                        case 0x73: //LD (HL),E
                            Write(regE, regHL);
                            opCycles += 8;
                            break;
                        case 0x74: //LD (HL),H
                            Write(regH, regHL);
                            opCycles += 8;
                            break;
                        case 0x75: //LD (HL),L
                            Write(regL, regHL);
                            opCycles += 8;
                            break;
                        case 0x76://HALT
                            halted = true;
                            opCycles += 4;
                            break;
                        case 0x77: //LD (HL),A
                            Write(regA, regHL);
                            opCycles += 8;
                            break;
                        case 0x78: //LD A,B
                            regA = regB;
                            opCycles += 4;
                            break;
                        case 0x79: //LD A,C
                            regA = regC;
                            opCycles += 4;
                            break;
                        case 0x7A: //LD A,D
                            regA = regD;
                            opCycles += 4;
                            break;
                        case 0x7B: //LD A,E
                            regA = regE;
                            opCycles += 4;
                            break;
                        case 0x7C: //LD A,H
                            regA = regH;
                            opCycles += 4;
                            break;
                        case 0x7D: //LD A,L
                            regA = regL;
                            opCycles += 4;
                            break;
                        case 0x7E: //LD A,(HL)
                            regA = Read(regHL);
                            opCycles += 8;
                            break;
                        case 0x7F: //LD A,A
                            opCycles += 4;
                            break;
                        case 0x80: //ADD A,B
                            regA = ADD(regA, regB);
                            opCycles += 4;
                            break;
                        case 0x81: //ADD A,C
                            regA = ADD(regA, regC);
                            opCycles += 4;
                            break;
                        case 0x82: //ADD A,D
                            regA = ADD(regA, regD);
                            opCycles += 4;
                            break;
                        case 0x83: //ADD A,E
                            regA = ADD(regA, regE);
                            opCycles += 4;
                            break;
                        case 0x84: //ADD A,H
                            regA = ADD(regA, regH);
                            opCycles += 4;
                            break;
                        case 0x85: //ADD A,L
                            regA = ADD(regA, regL);
                            opCycles += 4;
                            break;
                        case 0x86: //ADD A,(HL)
                            regA = ADD(regA, Read(regHL));
                            opCycles += 8;
                            break;
                        case 0x87: //ADD A,A
                            regA = ADD(regA, regA);
                            opCycles += 4;
                            break;
                        case 0x88: //ADC A,B
                            regA = ADC(regA, regB);
                            opCycles += 4;
                            break;
                        case 0x89: //ADC A,C
                            regA = ADC(regA, regC);
                            opCycles += 4;
                            break;
                        case 0x8A: //ADC A,D
                            regA = ADC(regA, regD);
                            opCycles += 4;
                            break;
                        case 0x8B: //ADC A,E
                            regA = ADC(regA, regE);
                            opCycles += 4;
                            break;
                        case 0x8C: //ADC A,H
                            regA = ADC(regA, regH);
                            opCycles += 4;
                            break;
                        case 0x8D: //ADC A,L
                            regA = ADC(regA, regL);
                            opCycles += 4;
                            break;
                        case 0x8E: //ADC A,(HL)
                            regA = ADC(regA, Read(regHL));
                            opCycles += 8;
                            break;
                        case 0x8F: //ADC A,A
                            regA = ADC(regA, regA);
                            opCycles += 4;
                            break;
                        case 0x90: //SUB A,B
                            regA = SUB(regA, regB);
                            opCycles += 4;
                            break;
                        case 0x91: //SUB A,C
                            regA = SUB(regA, regC);
                            opCycles += 4;
                            break;
                        case 0x92: //SUB A,D
                            regA = SUB(regA, regD);
                            opCycles += 4;
                            break;
                        case 0x93: //SUB A,E
                            regA = SUB(regA, regE);
                            opCycles += 4;
                            break;
                        case 0x94: //SUB A,H
                            regA = SUB(regA, regH);
                            opCycles += 4;
                            break;
                        case 0x95: //SUB A,L
                            regA = SUB(regA, regL);
                            opCycles += 4;
                            break;
                        case 0x96: //SUB A,(HL)
                            regA = SUB(regA, Read(regHL));
                            opCycles += 8;
                            break;
                        case 0x97: //SUB A,A
                            regA = SUB(regA, regA);
                            opCycles += 4;
                            break;
                        case 0x98: //SBC A,B
                            regA = SBC(regA, regB);
                            opCycles += 4;
                            break;
                        case 0x99: //SBC A,C
                            regA = SBC(regA, regC);
                            opCycles += 4;
                            break;
                        case 0x9A: //SBC A,D
                            regA = SBC(regA, regD);
                            opCycles += 4;
                            break;
                        case 0x9B: //SBC A,E
                            regA = SBC(regA, regE);
                            opCycles += 4;
                            break;
                        case 0x9C: //SBC A,H
                            regA = SBC(regA, regH);
                            opCycles += 4;
                            break;
                        case 0x9D: //SBC A,L
                            regA = SBC(regA, regL);
                            opCycles += 4;
                            break;
                        case 0x9E: //SBC A,(HL)
                            regA = SBC(regA, Read(regHL));
                            opCycles += 8;
                            break;
                        case 0x9F: //SBC A,A
                            regA = SBC(regA, regA);
                            opCycles += 4;
                            break;
                        case 0xA0: //AND A,B
                            regA = AND(regA, regB);
                            opCycles += 4;
                            break;
                        case 0xA1: //AND A,C
                            regA = AND(regA, regC);
                            opCycles += 4;
                            break;
                        case 0xA2: //AND A,D
                            regA = AND(regA, regD);
                            opCycles += 4;
                            break;
                        case 0xA3: //AND A,E
                            regA = AND(regA, regE);
                            opCycles += 4;
                            break;
                        case 0xA4: //AND A,H
                            regA = AND(regA, regH);
                            opCycles += 4;
                            break;
                        case 0xA5: //AND A,L
                            regA = AND(regA, regL);
                            opCycles += 4;
                            break;
                        case 0xA6: //AND A,(HL)
                            regA = AND(regA, Read(regHL));
                            opCycles += 8;
                            break;
                        case 0xA7: //AND A,A
                            regA = AND(regA, regA);
                            opCycles += 4;
                            break;
                        case 0xA8: //XOR A,B
                            regA = XOR(regA, regB);
                            opCycles += 4;
                            break;
                        case 0xA9: //XOR A,C
                            regA = XOR(regA, regC);
                            opCycles += 4;
                            break;
                        case 0xAA: //XOR A,D
                            regA = XOR(regA, regD);
                            opCycles += 4;
                            break;
                        case 0xAB: //XOR A,E
                            regA = XOR(regA, regE);
                            opCycles += 4;
                            break;
                        case 0xAC: //XOR A,H
                            regA = XOR(regA, regH);
                            opCycles += 4;
                            break;
                        case 0xAD: //XOR A,L
                            regA = XOR(regA, regL);
                            opCycles += 4;
                            break;
                        case 0xAE: //XOR A,(HL)
                            regA = XOR(regA, Read(regHL));
                            opCycles += 8;
                            break;
                        case 0xAF: //XOR A,A
                            regA = XOR(regA, regA);
                            opCycles += 4;
                            break;
                        case 0xB0: //OR A,B
                            regA = OR(regA, regB);
                            opCycles += 4;
                            break;
                        case 0xB1: //OR A,C
                            regA = OR(regA, regC);
                            opCycles += 4;
                            break;
                        case 0xB2: //OR A,D
                            regA = OR(regA, regD);
                            opCycles += 4;
                            break;
                        case 0xB3: //OR A,E
                            regA = OR(regA, regE);
                            opCycles += 4;
                            break;
                        case 0xB4: //OR A,H
                            regA = OR(regA, regH);
                            opCycles += 4;
                            break;
                        case 0xB5: //OR A,L
                            regA = OR(regA, regL);
                            opCycles += 4;
                            break;
                        case 0xB6: //OR A,(HL)
                            regA = OR(regA, Read(regHL));
                            opCycles += 8;
                            break;
                        case 0xB7: //OR A,A
                            regA = OR(regA, regA);
                            opCycles += 4;
                            break;
                        case 0xB8: //CP A,B
                            CP(regA, regB);
                            opCycles += 4;
                            break;
                        case 0xB9: //CP A,C
                            CP(regA, regC);
                            opCycles += 4;
                            break;
                        case 0xBA: //CP A,D
                            CP(regA, regD);
                            opCycles += 4;
                            break;
                        case 0xBB: //CP A,E
                            CP(regA, regE);
                            opCycles += 4;
                            break;
                        case 0xBC: //CP A,H
                            CP(regA, regH);
                            opCycles += 4;
                            break;
                        case 0xBD: //CP A,L
                            CP(regA, regL);
                            opCycles += 4;
                            break;
                        case 0xBE: //CP A,(HL)
                            CP(regA, Read(regHL));
                            opCycles += 8;
                            break;
                        case 0xBF: //CP A,A
                            CP(regA, regA);
                            opCycles += 4;
                            break;
                        case 0xC0: //RET NZ
                            if (!flagZ)
                            {
                                regPC = PopWordStack();
                                opCycles += 12;
                            }
                            opCycles += 8;
                            break;
                        case 0xC1: //POP BC
                            regBC = PopWordStack();
                            opCycles += 12;
                            break;
                        case 0xC2: //JP NZ,nn
                            tmp = ReadWord();
                            if (!flagZ)
                            {
                                regPC = tmp;
                                opCycles += 4;
                            }
                            opCycles += 12;
                            break;
                        case 0xC3: //JP nn
                            regPC = ReadWord();
                            opCycles += 16;
                            break;
                        case 0xC4: //CALL NZ,nn
                            tmp = ReadWord();
                            if (!flagZ)
                            {
                                PushWordStack(regPC);
                                regPC = tmp;
                                opCycles += 12;
                            }
                            opCycles += 12;
                            break;
                        case 0xC5: //PUSH BC
                            PushWordStack(regBC);
                            opCycles += 16;
                            break;
                        case 0xC6: //ADD A,n
                            regA = ADD(regA, Read());
                            opCycles += 8;
                            break;
                        case 0xC7: //RST 0
                            PushWordStack(regPC);
                            regPC = 0x0000;
                            opCycles += 16;
                            break;
                        case 0xC8: //RET Z
                            if (flagZ)
                            {
                                regPC = PopWordStack();
                                opCycles += 12;
                            }
                            opCycles += 8;
                            break;
                        case 0xC9: //RET
                            regPC = PopWordStack();
                            opCycles += 16;
                            break;
                        case 0xCA: //JP Z,nn
                            tmp = ReadWord();
                            if (flagZ)
                            {
                                regPC = tmp;
                                opCycles += 4;
                            }
                            opCycles += 12;
                            break;
                        case 0xCC: //CALL Z,nn
                            tmp = ReadWord();
                            if (flagZ)
                            {
                                PushWordStack(regPC);
                                regPC = tmp;
                                opCycles += 12;
                            }
                            opCycles += 12;
                            break;
                        case 0xCD: //CALL nn
                            tmp = ReadWord();
                            PushWordStack(regPC);
                            regPC = tmp;
                            opCycles += 24;
                            break;
                        case 0xCE: //ADC A,n
                            regA = ADC(regA, Read());
                            opCycles += 8;
                            break;
                        case 0xCF: //RST 8
                            PushWordStack(regPC);
                            regPC = 0x0008;
                            opCycles += 16;
                            break;
                        case 0xD0: //RET NC
                            if (!flagC)
                            {
                                regPC = PopWordStack();
                                opCycles += 12;
                            }
                            opCycles += 8;
                            break;
                        case 0xD1: //POP DE
                            regDE = PopWordStack();
                            opCycles += 12;
                            break;
                        case 0xD2: //JP NC,nn
                            tmp = ReadWord();
                            if (!flagC)
                            {
                                regPC = tmp;
                                opCycles += 4;
                            }
                            opCycles += 12;
                            break;
                        case 0xD3:
                            LockOP();
                            break;
                        case 0xD4: //CALL NC,nn
                            tmp = ReadWord();
                            if (!flagC)
                            {
                                PushWordStack(regPC);
                                regPC = tmp;
                                opCycles += 12;
                            }
                            opCycles += 12;
                            break;
                        case 0xD5: //PUSH DE
                            PushWordStack(regDE);
                            opCycles += 16;
                            break;
                        case 0xD6: //SUB A,n
                            regA = SUB(regA, Read());
                            opCycles += 8;
                            break;
                        case 0xD7: //RST 10
                            PushWordStack(regPC);
                            regPC = 0x0010;
                            opCycles += 16;
                            break;
                        case 0xD8: //RET C
                            if (flagC)
                            {
                                regPC = PopWordStack();
                                opCycles += 12;
                            }
                            opCycles += 8;
                            break;
                        case 0xD9: //RETI
                            IME = true;
                            regPC = PopWordStack();
                            opCycles += 16;
                            break;
                        case 0xDA: //JP C,nn
                            tmp = ReadWord();
                            if (flagC)
                            {
                                regPC = tmp;
                                opCycles += 4;
                            }
                            opCycles += 12;
                            break;
                        case 0xDB:
                            LockOP();
                            break;
                        case 0xDC: //CALL C,nn
                            tmp = ReadWord();
                            if (flagC)
                            {
                                PushWordStack(regPC);
                                regPC = tmp;
                                opCycles += 12;
                            }
                            opCycles += 12;
                            break;
                        case 0xDD:
                            LockOP();
                            break;
                        case 0xDE: //SBC A,n
                            regA = SBC(regA, Read());
                            opCycles += 8;
                            break;
                        case 0xDF: //RST 18
                            PushWordStack(regPC);
                            regPC = 0x0018;
                            opCycles += 16;
                            break;
                        case 0xE0: //LDH (n),A
                            Write(regA, 0xFF00 | Read());
                            opCycles += 12;
                            break;
                        case 0xE1: //POP HL
                            regHL = PopWordStack();
                            opCycles += 12;
                            break;
                        case 0xE2: //LDH (C),A
                            Write(regA, 0xFF00 | (regC & 0xFF));
                            opCycles += 8;
                            break;
                        case 0xE3:
                            LockOP();
                            break;
                        case 0xE4:
                            LockOP();
                            break;
                        case 0xE5: //PUSH HL
                            PushWordStack(regHL);
                            opCycles += 16;
                            break;
                        case 0xE6: //AND A,n
                            regA = AND(regA, Read());
                            opCycles += 8;
                            break;
                        case 0xE7: //RST 20
                            PushWordStack(regPC);
                            regPC = 0x0020;
                            opCycles += 16;
                            break;
                        case 0xE8: //ADD SP,d
                            tmp = (sbyte)Read();
                            otherTmp = regSP + tmp;
                            flagC = ((regSP ^ tmp ^ otherTmp) & 0x100) != 0;
                            flagH = ((regSP ^ tmp ^ otherTmp) & 0x10) != 0;
                            regSP = otherTmp & 0xFFFF;
                            flagZ = false;
                            flagN = false;
                            opCycles += 16;
                            break;
                        case 0xE9: //JP (HL)
                            regPC = regHL;
                            opCycles += 4;
                            break;
                        case 0xEA: //LD (nn),A
                            Write(regA, ReadWord());
                            opCycles += 16;
                            break;
                        case 0xEB:
                            LockOP();
                            break;
                        case 0xEC:
                            LockOP();
                            break;
                        case 0xED:
                            LockOP();
                            break;
                        case 0xEE: //XOR n
                            regA = XOR(regA, Read());
                            opCycles += 8;
                            break;
                        case 0xEF: //RST 28
                            PushWordStack(regPC);
                            regPC = 0x0028;
                            opCycles += 16;
                            break;
                        case 0xF0: //LDH A,(n)
                            regA = Read(0xFF00 | Read());
                            opCycles += 12;
                            break;
                        case 0xF1: //POP AF
                            regAF = PopWordStack();
                            opCycles += 12;
                            break;
                        case 0xF2: //LDH A,(C)
                            regA = Read(0xFF00 | (regC & 0xFF));
                            opCycles += 8;
                            break;
                        case 0xF3://DI
                            IME = false;
                            opCycles += 4;
                            break;
                        case 0xF4:
                            LockOP();
                            break;
                        case 0xF5: //PUSH AF
                            PushWordStack(regAF);
                            opCycles += 16;
                            break;
                        case 0xF6: //OR n
                            regA = OR(regA, Read());
                            opCycles += 8;
                            break;
                        case 0xF7: //RST 30
                            PushWordStack(regPC);
                            regPC = 0x0030;
                            opCycles += 16;
                            break;
                        case 0xF8: //LDHL SP,d
                            tmp = (sbyte)Read();
                            otherTmp = regSP + tmp;
                            flagC = ((regSP ^ tmp ^ otherTmp) & 0x100) != 0;
                            flagH = ((regSP ^ tmp ^ otherTmp) & 0x10) != 0;
                            regHL = otherTmp & 0xFFFF;
                            opCycles += 12;
                            flagZ = false;
                            flagN = false;
                            break;
                        case 0xF9: //LD SP,HL
                            regSP = regHL;
                            opCycles += 8;
                            break;
                        case 0xFA: //LD A,(nn)
                            regA = Read(ReadWord());
                            opCycles += 16;
                            break;
                        case 0xFB: //EI
                            IME = true;
                            opCycles += 4;
                            break;
                        case 0xFC:
                            LockOP();
                            break;
                        case 0xFD:
                            LockOP();
                            break;
                        case 0xFE: //CP n
                            CP(regA, Read());
                            opCycles += 8;
                            break;
                        case 0xFF: //RST 38
                            PushWordStack(regPC);
                            regPC = 0x0038;
                            opCycles += 16;
                            break;
                        case 0xCB: //Extra OPs
                            byte extraOP = Read();
                            int reg1 = 0;
                            int bit = 0;
                            bool HL = false;
                            bool bitOp = false;
                            switch (extraOP & 0x7)
                            {
                                case 0:
                                    reg1 = regB;
                                    break;
                                case 1:
                                    reg1 = regC;
                                    break;
                                case 2:
                                    reg1 = regD;
                                    break;
                                case 3:
                                    reg1 = regE;
                                    break;
                                case 4:
                                    reg1 = regH;
                                    break;
                                case 5:
                                    reg1 = regL;
                                    break;
                                case 6:
                                    reg1 = Read(regHL);
                                    HL = true;
                                    break;
                                case 7:
                                    reg1 = regA;
                                    break;
                            }
                            switch ((extraOP & 0xF8) >> 3)
                            {
                                case 0:
                                    reg1 = RLC(reg1);
                                    opCycles += 8;
                                    if (HL)
                                        opCycles += 8;
                                    break;
                                case 1:
                                    reg1 = RRC(reg1);
                                    opCycles += 8;
                                    if (HL)
                                        opCycles += 8;
                                    break;
                                case 2:
                                    reg1 = RL(reg1);
                                    opCycles += 8;
                                    if (HL)
                                        opCycles += 8;
                                    break;
                                case 3:
                                    reg1 = RR(reg1);
                                    opCycles += 8;
                                    if (HL)
                                        opCycles += 8;
                                    break;
                                case 4:
                                    reg1 = SLA(reg1);
                                    opCycles += 8;
                                    if (HL)
                                        opCycles += 8;
                                    break;
                                case 5:
                                    reg1 = SRA(reg1);
                                    opCycles += 8;
                                    if (HL)
                                        opCycles += 8;
                                    break;
                                case 6:
                                    reg1 = SWAP(reg1);
                                    opCycles += 8;
                                    if (HL)
                                        opCycles += 8;
                                    break;
                                case 7:
                                    reg1 = SRL(reg1);
                                    opCycles += 8;
                                    if (HL)
                                        opCycles += 8;
                                    break;
                                default:
                                    switch (((extraOP & 0xF8) >> 3) & 7)
                                    {
                                        case 0:
                                            bit = 1;
                                            break;
                                        case 1:
                                            bit = 2;
                                            break;
                                        case 2:
                                            bit = 4;
                                            break;
                                        case 3:
                                            bit = 8;
                                            break;
                                        case 4:
                                            bit = 0x10;
                                            break;
                                        case 5:
                                            bit = 0x20;
                                            break;
                                        case 6:
                                            bit = 0x40;
                                            break;
                                        case 7:
                                            bit = 0x80;
                                            break;
                                    }
                                    switch ((extraOP & 0xC0) >> 6)
                                    {
                                        case 1:
                                            bitOp = true;
                                            BIT(reg1, bit);
                                            opCycles += 8;
                                            if (HL)
                                                opCycles += 4;
                                            break;
                                        case 2:
                                            reg1 = RES(reg1, bit);
                                            opCycles += 8;
                                            if (HL)
                                                opCycles += 4;
                                            break;
                                        case 3:
                                            reg1 = SET(reg1, bit);
                                            opCycles += 8;
                                            if (HL)
                                                opCycles += 8;
                                            break;
                                    }
                                    break;
                            }
                            switch (extraOP & 0x7)
                            {
                                case 0:
                                    regB = reg1;
                                    break;
                                case 1:
                                    regC = reg1;
                                    break;
                                case 2:
                                    regD = reg1;
                                    break;
                                case 3:
                                    regE = reg1;
                                    break;
                                case 4:
                                    regH = reg1;
                                    break;
                                case 5:
                                    regL = reg1;
                                    break;
                                case 6:
                                    if(!bitOp)
                                        Write(reg1, regHL);
                                    break;
                                case 7:
                                    regA = reg1;
                                    break;
                            }
                            break;
                        default:
                            LockOP();
                            break;
                    }
                }
#endregion cpu
                lcd.AddCycles(opCycles >> (cpuClock == CGBSPEED ? 1 : 0));
                TimerClock(opCycles);
                DividerClock(opCycles);
                if (InterVBlank || InterLCDSTAT || InterTimer || InterSerial || InterJoypad) //I can't figure out if IME needs to be true for halt to stop : /. but this passes Blargg.
                    halted = false;
                if (IME)
                {
                    if (InterVBlank && InterVBlankEnabled)
                    {
                        InterVBlank = false;
                        IME = false;
                        PushWordStack(regPC);
                        regPC = 0x40;
                    }
                    else if (InterLCDSTAT && InterLCDSTATEnabled)
                    {
                        InterLCDSTAT = false;
                        IME = false;
                        PushWordStack(regPC);
                        regPC = 0x48;
                    }
                    else if (InterTimer && InterTimerEnabled)
                    {
                        InterTimer = false;
                        IME = false;
                        PushWordStack(regPC);
                        regPC = 0x50;
                    }
                    else if (InterSerial && InterSerialEnabled)
                    {
                        InterSerial = false;
                        IME = false;
                        PushWordStack(regPC);
                        regPC = 0x58;
                    }
                    else if (InterJoypad && InterJoypadEnabled)
                    {
                        InterJoypad = false;
                        IME = false;
                        PushWordStack(regPC);
                        regPC = 0x60;
                    }
                }
                if (opCycles == 0)
                    serialData += " No Cycles " + opCode.ToString("X2");
                cycles += opCycles;
            }
        }
        private void PushStack(int value)
        {
            regSP = (regSP - 1) & 0xFFFF;
            Write(value, regSP);
        }
        private void PushWordStack(int value)
        {
            PushStack(value >> 8);
            PushStack(value);
        }
        private byte PopStack()
        {
            byte value = Read(regSP);
            regSP = (regSP + 1) & 0xFFFF;
            return value;
        }
        private ushort PopWordStack()
        {
            return (ushort)(PopStack() | (PopStack() << 8));
        }
        private byte Read(int address)
        {
            address = address & 0xFFFF;
            byte nextByte = 0;
            if (address >= 0xE000 && address < 0xFE00)//ECHO
            {
                address = address - 0x1000;
            }
            if (address >= 0xFE00 && address < 0xFEA0)
            {
                nextByte = oamRam[address & 0xFF];
            }
            else if (address >= 0xFF00)//IO Ports
            {
                switch (address)
                {
                    case 0xFF00://P1/JOYP - Joypad (R/W)
                        nextByte = 0xF;
                        if ((selectDirections && input.down) || (selectButtons && input.start))
                            nextByte &= 0xF7;
                        if ((selectDirections && input.up) || (selectButtons && input.select))
                            nextByte &= 0xFB;
                        if ((selectDirections && input.left) || (selectButtons && input.b))
                            nextByte &= 0xFD;
                        if ((selectDirections && input.right) || (selectButtons && input.a))
                            nextByte &= 0xFE;
                        if (!selectDirections)
                            nextByte |= 0x10;
                        if (!selectButtons)
                            nextByte |= 0x20;
                        if (rom.SGB)
                        {
                            if (sgb.Read() != 0xF) //Only support 1 player so far
                                nextByte |= 0x0F;
                            if (!selectButtons && !selectDirections)
                                nextByte = (byte)((nextByte & 0xF0) | ((byte)sgb.Read()));
                        }
                        break;
                    case 0xFF02://SC - Serial Transfer Control (R/W)
                        break;
                    case 0xFF04://DIV - Divider Register (R/W)
                        nextByte = divider;
                        break;
                    case 0xFF06://TMA - Timer Modulo (R/W)
                        nextByte = timerReload;
                        break;
                    case 0xFF07://TAC - Timer Control (R/W)
                        nextByte = (byte)(timerClockMode | (timerEnabled ? 0x4 : 0x0));
                        break;
                    case 0xFF0F://IF - Interrupt Flag (R/W)
                        if (InterVBlank)
                            nextByte |= 1;
                        if (InterLCDSTAT)
                            nextByte |= 2;
                        if (InterTimer)
                            nextByte |= 4;
                        if (InterSerial)
                            nextByte |= 8;
                        if (InterJoypad)
                            nextByte |= 0x10;
                        break;
                    case 0xFF41://STAT - LCDC Status (R/W)
                        nextByte |= lcd.mode;
                        nextByte |= (byte)((LYC == LY) ? 0x4 : 0);
                        nextByte |= (byte)(InterSTATHBlankEnabled ? 0x8 : 0);
                        nextByte |= (byte)(InterSTATVBlankEnabled ? 0x10 : 0);
                        nextByte |= (byte)(InterSTATOAMEnabled ? 0x20 : 0);
                        nextByte |= (byte)(InterSTATCoincidenceEnabled ? 0x40 : 0);
                        break;
                    case 0xFF44://LY - LCDC Y-Coordinate (R)
                        nextByte = LY;
                        break;
                    case 0xFF4D://KEY1 - CGB Mode Only - Prepare Speed Switch
                        if (rom.cgbMode)
                        {
                            nextByte |= (byte)(pendingSpeedChange ? 1 : 0);
                            nextByte |= (byte)(cpuClock != NORMALSPEED ? 0x80 : 0);
                        }
                        break;
                    case 0xFF51://HDMA1 - CGB Mode Only - New DMA Source, High
                        if (rom.cgbMode)
                            nextByte = (byte)(DMASourceAddress >> 8);
                        break;
                    case 0xFF52://HDMA2 - CGB Mode Only - New DMA Source, Low
                        if (rom.cgbMode)
                            nextByte = (byte)(DMASourceAddress & 0xFF);
                        break;
                    case 0xFF53://HDMA3 - CGB Mode Only - New DMA Destination, High
                        if (rom.cgbMode)
                            nextByte = (byte)(DMADstAddress >> 8);
                        break;
                    case 0xFF54://HDMA4 - CGB Mode Only - New DMA Destination, Low
                        if (rom.cgbMode)
                            nextByte = (byte)(DMADstAddress & 0xFF);
                        break;
                    case 0xFF55://HDMA5 - CGB Mode Only - New DMA Length/Mode/Start
                        if (rom.cgbMode)
                        {
                            if (!DMAActive)
                                nextByte = 0xFF;
                            else
                            {
                                if (DMAHBlank)
                                {
                                    nextByte = (byte)(((DMALength - DMAPosition - 1) / 10) & 0x7F);
                                }
                            }
                        }
                        break;
                    case 0xFF68://BCPS/BGPI - CGB Mode Only - Background Palette Index
                        if (rom.cgbMode)
                        {
                            nextByte = (byte)(cgbBGPIndex & 0x3F);
                            nextByte |= (byte)(cgbBGPAuto ? 0x80 : 0x00);
                            nextByte |= 0x40;
                        }
                        break;
                    case 0xFF69://BCPD/BGPD - CGB Mode Only - Background Palette Data
                        if (rom.cgbMode)
                        {
                            nextByte = cgbBGP[cgbBGPIndex];
                        }
                        break;
                    case 0xFF6A://OCPS/OBPI - CGB Mode Only - Sprite Palette Index
                        if (rom.cgbMode)
                        {
                            nextByte = (byte)(cgbOBPIndex & 0x3F);
                            nextByte |= (byte)(cgbOBPAuto ? 0x80 : 0x00);
                            nextByte |= 0x40;
                        }
                        break;
                    case 0xFF6B://OCPD/OBPD - CGB Mode Only - Sprite Palette Data
                        if (rom.cgbMode)
                        {
                            nextByte = cgbOBP[cgbOBPIndex];
                        }
                        break;
                    case 0xFF70://SVBK - CGB Mode Only - WRAM Bank
                        if (rom.cgbMode)
                        {
                            nextByte = (byte)(wramBank & 0xFF);
                        }
                        break;
                    case 0xFFFF://IE - Interrupt Enable (R/W)
                        if (InterVBlankEnabled)
                            nextByte |= 1;
                        if (InterLCDSTATEnabled)
                            nextByte |= 2;
                        if (InterTimerEnabled)
                            nextByte |= 4;
                        if (InterSerialEnabled)
                            nextByte |= 8;
                        if (InterJoypadEnabled)
                            nextByte |= 0x10;
                        break;
                    default://HRAM
                            nextByte = hRam[address & 0xFF];
                        break;
                }
            }
            else 
                nextByte = memory[address];
            return nextByte;
        }
        private byte Read()
        {
            return Read(regPC++);
        }
        private ushort ReadWord()
        {
            return (ushort)(Read() | (Read() << 8));
        }
        private ushort ReadWord(int address)
        {
            return (ushort)(Read(address) | (Read((address + 1) & 0xFFFF) << 8));
        }
        private void Write(int value, int address)
        {
            address = address & 0xFFFF;
            if (address >= 0xE000 && address < 0xFE00)//ECHO
            {
                address = address - 0x1000;
            }
            if (address >= 0xFE00 && address < 0xFEA0)
            {
                oamRam[address & 0xFF] = (byte)value;
            }
            else if (address >= 0xFF00)//IO Ports
            {
                switch (address)
                {
                    case 0xFF00://P1/JOYP - Joypad (R/W)
                        selectButtons = (value & 0x20) == 0;
                        selectDirections = (value & 0x10) == 0;
                        if (rom.SGB)
                            sgb.Write(value);
                        break;
                    case 0xFF01://SB - Serial transfer data (R/W)
                        serialByte = (byte)(value & 0xFF);
                        break;
                    case 0xFF02://SC - Serial Transfer Control (R/W)
                        if (value == 0x81)
                            serialData += (char)serialByte;
                        break;
                    case 0xFF04://DIV - Divider Register (R/W)
                        divider = 0;
                        break;
                    case 0xFF05://TIMA - Timer counter (R/W)
                        timer = (byte)(value & 0xFF);
                        break;
                    case 0xFF06://TMA - Timer Modulo (R/W)
                        timerReload = (byte)(value & 0xFF);
                        break;
                    case 0xFF07://TAC - Timer Control (R/W)
                        timerEnabled = (value & 4) != 0;
                        timerClockMode = (byte)(value & 3);
                        switch (timerClockMode)
                        {
                            case 0:
                                timerClock = cpuClock / 4096;
                                break;
                            case 1:
                                timerClock = cpuClock / 262144;
                                break;
                            case 2:
                                timerClock = cpuClock / 65536;
                                break;
                            case 3:
                                timerClock = cpuClock / 16384;
                                break;
                        }
                        break;
                    case 0xFF0F://IF - Interrupt Flag (R/W)
                        InterVBlank = (value & 1) != 0;
                        InterLCDSTAT = (value & 2) != 0;
                        InterTimer = (value & 4) != 0;
                        InterSerial = (value & 8) != 0;
                        InterJoypad = (value & 0x10) != 0;
                        break;
                    case 0xFF40://LCDC - LCD Control (R/W)
                        LCDC = (byte)(value & 0xFF);
                        break;
                    case 0xFF41://STAT - LCDC Status (R/W)
                        InterSTATHBlankEnabled = (value & 0x08) != 0;
                        InterSTATVBlankEnabled = (value & 0x10) != 0;
                        InterSTATOAMEnabled = (value & 0x20) != 0;
                        InterSTATCoincidenceEnabled = (value & 0x40) != 0;
                        break;
                    case 0xFF42://SCY - Scroll Y (R/W)
                        SCY = (byte)(value & 0xFF);
                        break;
                    case 0xFF43://SCX - Scroll X (R/W)
                        SCX = (byte)(value & 0xFF);
                        break;
                    case 0xFF44://LY - LCDC Y-Coordinate (R)
                        LY = 0;
                        break;
                    case 0xFF45://LYC - LY Compare (R/W)
                        LYC = (byte)(value & 0xFF);
                        break;
                    case 0xFF46://DMA - DMA Transfer and Start Address (W)
                        for (int i = 0; i < 0xA0; i++)
                        {
                            oamRam[i] = memory[(value << 8) + i];
                        }
                        break;
                    case 0xFF47://BGP - BG Palette Data (R/W)
                        BGP[0] = value & 3;
                        BGP[1] = (value >> 2) & 3;
                        BGP[2] = (value >> 4) & 3;
                        BGP[3] = (value >> 6) & 3;
                        break;
                    case 0xFF48://OBP0 - Object Palette 0 Data (R/W)
                        OBP0[0] = value & 3;
                        OBP0[1] = (value >> 2) & 3;
                        OBP0[2] = (value >> 4) & 3;
                        OBP0[3] = (value >> 6) & 3;
                        break;
                    case 0xFF49://OBP1 - Object Palette 1 Data (R/W)
                        OBP1[0] = value & 3;
                        OBP1[1] = (value >> 2) & 3;
                        OBP1[2] = (value >> 4) & 3;
                        OBP1[3] = (value >> 6) & 3;
                        break;
                    case 0xFF4A://WY - Window Y Position (R/W)
                        WY = (byte)(value & 0xFF);
                        break;
                    case 0xFF4B://WX - Window X Position (R/W)
                        WX = (byte)(value & 0xFF);
                        break;
                    case 0xFF4D://KEY1 - CGB Mode Only - Prepare Speed Switch
                        if (rom.cgbMode)
                            pendingSpeedChange = (value & 1) != 0;
                        break;
                    case 0xFF4F://VBK - CGB Mode Only - VRAM Bank
                        if (rom.cgbMode)
                            memory.SwapVRAM(value & 1);
                        break;
                    case 0xFF51://HDMA1 - CGB Mode Only - New DMA Source, High
                        if (rom.cgbMode && !DMAActive)
                            DMASourceAddress = (DMASourceAddress & 0xFF) | (value << 8);
                        break;
                    case 0xFF52://HDMA2 - CGB Mode Only - New DMA Source, Low
                        if (rom.cgbMode && !DMAActive)
                            DMASourceAddress = (DMASourceAddress & 0xFF00) | (value & 0xF0);
                        break;
                    case 0xFF53://HDMA3 - CGB Mode Only - New DMA Destination, High
                        if (rom.cgbMode && !DMAActive)
                            DMADstAddress = (DMADstAddress & 0xFF) | (((value & 0x1F) | 0x80) << 8);
                        break;
                    case 0xFF54://HDMA4 - CGB Mode Only - New DMA Destination, Low
                        if (rom.cgbMode && !DMAActive)
                            DMADstAddress = (DMADstAddress & 0xFF00) | (value & 0xF0);
                        break;
                    case 0xFF55://HDMA5 - CGB Mode Only - New DMA Length/Mode/Start
                        if (rom.cgbMode)
                        {
                            DMAActive = true;
                            DMAHBlank = (value & 0x80) != 0;
                            DMALength = ((value & 0x7F) + 1) * 10;
                            if (!DMAHBlank)
                            {
                                while(DMAPosition < DMALength)
                                {
                                    memory[DMADstAddress + DMAPosition] = memory[DMASourceAddress + DMAPosition];
                                    DMAPosition++;
                                }
                                DMAActive = false;
                            }
                        }
                        break;
                    case 0xFF68://BCPS/BGPI - CGB Mode Only - Background Palette Index
                        if (rom.cgbMode)
                        {
                            cgbBGPIndex = value & 0x3F;
                            cgbBGPAuto = (value & 0x80) != 0;
                        }
                        break;
                    case 0xFF69://BCPD/BGPD - CGB Mode Only - Background Palette Data
                        if (rom.cgbMode)
                        {
                            cgbBGP[cgbBGPIndex] = (byte)(value & 0xFF);
                            if (cgbBGPAuto)
                                cgbBGPIndex = (cgbBGPIndex + 1) & 0x3F;
                        }
                        break;
                    case 0xFF6A://OCPS/OBPI - CGB Mode Only - Sprite Palette Index
                        if (rom.cgbMode)
                        {
                            cgbOBPIndex = value & 0x3F;
                            cgbOBPAuto = (value & 0x80) != 0;
                        }
                        break;
                    case 0xFF6B://OCPD/OBPD - CGB Mode Only - Sprite Palette Data
                        if (rom.cgbMode)
                        {
                            cgbOBP[cgbOBPIndex ] = (byte)(value & 0xFF);
                            if (cgbOBPAuto)
                                cgbOBPIndex = (cgbOBPIndex + 1) & 0x3F;
                        }
                        break;
                    case 0xFF70://SVBK - CGB Mode Only - WRAM Bank
                        if (rom.cgbMode)
                        {
                            wramBank = value & 7;
                            memory.SwapWRAM(wramBank);
                        }
                        break;
                    case 0xFFFF://IE - Interrupt Enable (R/W)
                        InterVBlankEnabled = (value & 1) != 0;
                        InterLCDSTATEnabled = (value & 2) != 0;
                        InterTimerEnabled = (value & 4) != 0;
                        InterSerialEnabled = (value & 8) != 0;
                        InterJoypadEnabled = (value & 0x10) != 0;
                        break;
                }
                hRam[address & 0xFF] = (byte)(value & 0xFF);
            }
            else if (address < 0xE000)
            {
                mapper.Write((byte)(value & 0xFF), (ushort)address);
                memory[address] = (byte)(value & 0xFF);
            }
        }
        private void WriteWord(int value, int address)
        {
            Write(value, address);
            Write(value >> 8, address + 1);
        }
        private void LockOP() //Missing op should lock up the gameboy CPU when encountered;
        {
            serialData += "Lock ";
        }
        private void DividerClock(int cycles)
        {
            for (int i = 0; i < cycles; i++)
            {
                dividerClocker++;
                if (dividerClocker >= cpuClock / 16384)
                {
                    divider++;
                    dividerClocker = 0;
                }
            }
        }
        private void TimerClock(int cycles)
        {
            if (timerEnabled)
            {
                for (int i = 0; i < cycles; i++)
                {
                    timerClocker++;
                    if (timerClocker >= timerClock)
                    {
                        timer++;
                        timerClocker = 0;
                        if (timer == 0)
                        {
                            timer = timerReload;
                            InterTimer = true;
                        }
                    }
                }
            }
        }
        private int JR(int n, int reg1)
        {
            if ((n & 0x80) != 0)
            {
                n ^= 0xFF;
                n++;
                reg1 -= n;
            }
            else
            {
                reg1 += n;
            }
            return reg1 & 0xFFFF;
        }
        private int INC(int reg)
        {
            reg++;
            reg &= 0xFF;
            intZ = reg;
            flagN = false;
            flagH = ((reg & 0xF) == 0);
            return reg;
        }
        private int DEC(int reg)
        {
            reg--;
            reg &= 0xFF;
            intZ = reg;
            flagN = true;
            flagH = ((reg & 0xF) == 0xF);
            return reg;
        }
        private int ADD16(int reg1, int reg2)
        {
            int reg3 = (reg1 + reg2);
            flagN = false;
            flagH = ((reg1 ^ reg2 ^ (reg3 & 0xFFFF)) & 0x1000) != 0; //I don't really get how the H flag works here, this is from VBA source.
            flagC = reg3 > 0xFFFF;
            return reg3 & 0xFFFF;
        }
        private int ADD(int reg1, int reg2)
        {
            int reg3 = reg1 + reg2;
            flagN = false;
            flagC = reg3 > 0xFF;
            flagH = ((reg1 & 0xF) + (reg2 & 0xF)) > 0xF;
            return intZ = reg3 & 0xFF;
        }
        private int ADC(int reg1, int reg2)
        {
            int reg3 = reg1 + reg2 + (flagC ? 1 : 0);
            flagN = false;
            flagH = ((reg1 & 0xF) + (reg2 & 0xF) + (flagC ? 1 : 0)) > 0xF;
            flagC = reg3 > 0xFF;
            return intZ = reg3 & 0xFF;
        }
        private int SUB(int reg1, int reg2)
        {
            int reg3 = reg1 - reg2;
            flagN = true;
            flagC = reg3 < 0x0;
            flagH = ((reg1 & 0xF) - (reg2 & 0xF)) < 0x0;
            return intZ = reg3 & 0xFF;
        }
        private int SBC(int reg1, int reg2)
        {
            int reg3 = reg1 - reg2 - (flagC ? 1 : 0);
            flagN = true;
            flagH = ((reg1 & 0xF) - (reg2 & 0xF) - (flagC ? 1 : 0)) < 0x0;
            flagC = reg3 < 0x0;
            return intZ = reg3 & 0xFF;
        }
        private int AND(int reg1, int reg2)
        {
            flagN = false;
            flagH = true;
            flagC = false;
            return intZ = (reg1 & reg2) & 0xFF;
        }
        private int XOR(int reg1, int reg2)
        {
            flagN = false;
            flagH = false;
            flagC = false;
            return intZ = (reg1 ^ reg2) & 0xFF;
        }
        private int OR(int reg1, int reg2)
        {
            flagN = false;
            flagH = false;
            flagC = false;
            return intZ = (reg1 | reg2) & 0xFF;
        }
        private void CP(int reg1, int reg2)
        {
            int reg3 = reg1 - reg2;
            flagN = true;
            flagC = reg3 < 0x0;
            flagH = ((reg1 & 0xF) - (reg2 & 0xF)) < 0x0;
            intZ = reg3 & 0xFF;
        }
        private int RLC(int reg1)
        {
            if ((reg1 & 0x80) != 0)
            {
                flagC = true;
                reg1 = ((reg1 << 1) | 1) & 0xFF;
            }
            else
            {
                flagC = false;
                reg1 = (reg1 << 1) & 0xFF;
            }
            flagH = false;
            flagN = false;
            intZ = reg1;
            return reg1;
        }
        private int RRC(int reg1)
        {
            if ((reg1 & 0x1) != 0)
            {
                flagC = true;
                reg1 = ((reg1 >> 1) | 0x80) & 0xFF;
            }
            else
            {
                flagC = false;
                reg1 = (reg1 >> 1) & 0xFF;
            }
            flagH = false;
            flagN = false;
            intZ = reg1;
            return reg1;
        }
        private int RL(int reg1)
        {
            int reg2 = ((reg1 << 1) | (flagC ? 1 : 0)) & 0xFF;
            flagC = (reg1 & 0x80) != 0;
            flagH = false;
            flagN = false;
            intZ = reg2;
            return reg2;
        }
        private int RR(int reg1)
        {
            int reg2 = ((reg1 >> 1) | (flagC ? 0x80 : 0x00)) & 0xFF;
            flagC = (reg1 & 0x1) != 0;
            flagH = false;
            flagN = false;
            intZ = reg2;
            return reg2;
        }
        private int SLA(int reg1)
        {
            int reg2 = (reg1 << 1) & 0xFF;
            flagC = (reg1 & 0x80) != 0;
            flagH = false;
            flagN = false;
            intZ = reg2;
            return reg2;
        }
        private int SRA(int reg1)
        {
            int reg2 =((reg1 >> 1) | (reg1 & 0x80)) & 0xFF;
            flagC = (reg1 & 0x1) != 0;
            flagH = false;
            flagN = false;
            intZ = reg2;
            return reg2;
        }
        private int SWAP(int reg1)
        {
            int reg2 = (((reg1 & 0x0F) << 4) | ((reg1 & 0xF0) >> 4)) & 0xFF;
            flagC = false;
            flagH = false;
            flagN = false;
            intZ = reg2;
            return reg2;
        }
        private int SRL(int reg1)
        {
            int reg2 = (reg1 >> 1) & 0xFF;
            flagC = (reg1 & 0x1) != 0;
            flagH = false;
            flagN = false;
            intZ = reg2;
            return reg2;
        }
        private void BIT(int reg1, int bit)
        {
            intZ = reg1 & bit;
            flagH = true;
            flagN = false;
        }
        private int RES(int reg1, int bit)
        {
            return (reg1 & (bit ^ 0xFF));
        }
        private int SET(int reg1, int bit)
        {
            return (reg1 | bit);
        }
        public byte[] GetRam()
        {
            byte[] sram = new byte[rom.ramSize * 1024];

            for (int i = 0x00; i < rom.ramSize * 0x400; i++)
                sram[i] = memory.banks[(i / 0x400) + memory.ramSwapOffset][i % 0x400];
            return sram;
        }
        public void SetRam(byte[] sram)
        {
            for (int i = 0x00; i < rom.ramSize * 0x400; i++)
                 memory.banks[(i / 0x400) + memory.ramSwapOffset][i % 0x400] = sram[i];
        }
    }
}
