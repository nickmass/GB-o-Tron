using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gb_o_tron
{
    public class Audio
    {
        GBCore gb;

        public int sampleRate;
        private double sampleRateDivider;
        private double sampleDivider;

        private bool[][] dutyCycles = {
            new bool[] {true, false, false, false, false, false, false, false},
            new bool[] {true, true, false, false, false, false, false, false},
            new bool[] {true, true, true, true, false, false, false, false},
            new bool[] {true, true, true, true, true, true, false, false}
        };
        int frameCounter;
        int frameCounterLength = 8192;
        int frameStep = 0;

        int square1Volume = 0;
        int square1SweepCounter;
        public int square1LengthCounter;
        int square1EnvelopeCounter;
        int square1Counter;
        int square1DutyCounter;
        int square1Freq;
        int square1ShadowFreq;
        bool square1SweepMute;

        int square2Volume = 0;
        public int square2LengthCounter;
        int square2EnvelopeCounter;
        int square2Counter;
        int square2DutyCounter;

        int wavVolume = 0;
        public int wavLengthCounter;
        int wavCounter;
        int wavAddr;

        int noiseVolume = 0;
        public int noiseLengthCounter;
        int noiseEnvelopeCounter;
        int noiseCounter;
        bool noise;
        int shiftReg = 1;

        public byte[] output;
        public int outputPTR;

        int leftSampleTotal;
        int rightSampleTotal;
        int sampleCount;

        private Queue<double> rollingAveRight = new Queue<double>();
        private Queue<double> rollingAveLeft = new Queue<double>();
        private double rollingAveTotalRight;
        private double rollingAveTotalLeft;
        private long rollingAveCount;
        private long rollingAveWindow;

        public Audio(GBCore gb, int sampleRate)
        {
            this.gb = gb;
            output = new byte[sampleRate * 2];
            this.sampleRate = sampleRate;
            rollingAveWindow = this.sampleRate / 10;
            sampleDivider = (gb.NORMALSPEED / 60.0D) / ((sampleRate * 1.0) / 60.0D);
        }

        public void ResetBuffers()
        {
            outputPTR = 0;
        }

        public void Square1Sweep()
        {
            if (gb.square1SweepEnabled)
            {
                if (square1SweepCounter > 0)
                    square1SweepCounter--;
                if (square1SweepCounter == 0)
                {
                    int newFreq = gb.square1Timer >> gb.square1SweepShift;
                    if (!gb.square1SweepDirection)
                        newFreq = newFreq * -1;
                    newFreq += square1ShadowFreq;
                    if (newFreq > 2047 || newFreq <= 0)
                        square1SweepMute = true;
                    else
                    {
                        square1SweepMute = false;
                        square1ShadowFreq = newFreq;
                        gb.square1Timer = newFreq;
                    }
                    square1SweepCounter = gb.square1SweepFreq;
                }
            }
        }
        public void Square1Envelope()
        {
            if (gb.square1EnvelopeEnabled)
            {
                if (square1EnvelopeCounter > 0)
                    square1EnvelopeCounter--;
                if (square1EnvelopeCounter == 0)
                {
                    if (square1Volume > 0 && !gb.square1EnvelopeDirection)
                        square1Volume--;
                    if (square1Volume < 0xF && gb.square1EnvelopeDirection)
                        square1Volume++;
                    square1EnvelopeCounter = gb.square1EnvelopeFreq;
                }
            }
        }

        public void Square1Length()
        {
            if (square1LengthCounter > 0)
                square1LengthCounter--;
            if (square1LengthCounter == 0 && gb.square1Loop)
            {
                square1LengthCounter = gb.square1Length;
            }
        }

        public void Square2Envelope()
        {
            if (gb.square2EnvelopeEnabled)
            {
                if (square2EnvelopeCounter > 0)
                    square2EnvelopeCounter--;
                if (square2EnvelopeCounter == 0)
                {
                    if (square2Volume > 0 && !gb.square2EnvelopeDirection)
                        square2Volume--;
                    if (square2Volume < 0xF && gb.square2EnvelopeDirection)
                        square2Volume++;
                    square2EnvelopeCounter = gb.square2EnvelopeFreq;
                }
            }
        }

        public void Square2Length()
        {
            if (square2LengthCounter > 0)
                square2LengthCounter--;
            if (square2LengthCounter == 0 && gb.square2Loop)
            {
                square2LengthCounter = gb.square2Length;
            }
        }

        public void WavLength()
        {
            if (gb.wavEnabled)
            {
                if (wavLengthCounter > 0)
                    wavLengthCounter--;
                if (wavLengthCounter == 0 && gb.wavLoop)
                {
                    wavLengthCounter = gb.wavLength;
                }
            }
        }

        public void NoiseEnvelope()
        {
            if (gb.noiseEnvelopeEnabled)
            {
                if (noiseEnvelopeCounter > 0)
                    noiseEnvelopeCounter--;
                if (noiseEnvelopeCounter == 0)
                {
                    if (noiseVolume > 0 && !gb.noiseEnvelopeDirection)
                        noiseVolume--;
                    if (noiseVolume < 0xF && gb.noiseEnvelopeDirection)
                        noiseVolume++;
                    noiseEnvelopeCounter = gb.noiseEnvelopeFreq;
                }
            }
        }

        public void NoiseLength()
        {
            if (noiseLengthCounter > 0)
                noiseLengthCounter--;
            if (noiseLengthCounter == 0 && gb.noiseLoop)
            {
                noiseLengthCounter = gb.noiseLength;
            }
        }
        public void AddCycles(int cycles)
        {
            for (int currCycle = 0; currCycle < cycles; currCycle++)
            {
                if (gb.soundEnabled)
                {
                    if (gb.square1Reset)
                    {
                        square1LengthCounter = gb.square1Length;
                        if (square1LengthCounter == 0)
                            square1LengthCounter = 64;
                        square1SweepMute = false;
                        square1ShadowFreq = gb.square1Timer;
                        square1Counter = (2048 - gb.square1Timer) * 4;
                        square1EnvelopeCounter = gb.square1EnvelopeFreq;
                        square1Volume = gb.square1Volume;
                        square1SweepCounter = gb.square1SweepFreq;
                        //square1Freq = gb.square1Freq;
                        gb.square1Reset = false;
                    }

                    if (gb.square2Reset)
                    {
                        square2LengthCounter = gb.square2Length;
                        if (square2LengthCounter == 0)
                            square2LengthCounter = 64;
                        square2Counter = gb.square2Freq;
                        square2EnvelopeCounter = gb.square2EnvelopeFreq;
                        square2Volume = gb.square2Volume;
                        gb.square2Reset = false;
                    }

                    if (gb.wavReset)
                    {
                        wavLengthCounter = gb.wavLength;
                        if (wavLengthCounter == 0)
                            wavLengthCounter = 256;
                        wavCounter = gb.wavFreq;
                        wavAddr = 0;
                        gb.wavReset = false;
                    }

                    if (gb.noiseReset)
                    {
                        noiseLengthCounter = gb.noiseLength;
                        if (noiseLengthCounter == 0)
                            noiseLengthCounter = 64;
                        noiseCounter = gb.noiseShiftFreq;
                        noiseEnvelopeCounter = gb.noiseEnvelopeFreq;
                        noiseVolume = gb.noiseVolume;
                        gb.noiseReset = false;
                        shiftReg = 0x7FFF;
                    }

                    if(frameCounter > 0)
                        frameCounter--;
                    if (frameCounter == 0)
                    {
                        frameStep++;
                        switch (frameStep % 8)
                        {
                            case 0://128
                                Square1Length();
                                Square2Length();
                                WavLength();
                                NoiseLength();
                                break;
                            case 2://128 64
                                Square1Sweep();
                                Square1Length();
                                Square2Length();
                                WavLength();
                                NoiseLength();
                                break;
                            case 4://128
                                Square1Length();
                                Square2Length();
                                WavLength();
                                NoiseLength();
                                break;
                            case 6://128 64
                                Square1Sweep();
                                Square1Length();
                                Square2Length();
                                WavLength();
                                NoiseLength();
                                break;
                            case 7://256
                                Square1Envelope();
                                Square2Envelope();
                                NoiseEnvelope();
                                break;
                        }
                        frameCounter = frameCounterLength;
                    }

                    if(square1Counter > 0)
                        square1Counter--;
                    if (square1Counter == 0 && square1LengthCounter != 0)
                    {
                        square1DutyCounter++;
                        square1Counter = (2048 - gb.square1Timer) * 4;
                    }

                    if (square2Counter > 0)
                        square2Counter--;
                    if (square2Counter == 0 && square2LengthCounter != 0)
                    {
                        square2DutyCounter++;
                        square2Counter = gb.square2Freq;
                    }

                    if (gb.wavEnabled)
                    {
                        if (wavCounter > 0)
                            wavCounter--;
                        if (wavCounter == 0 && wavLengthCounter != 0)
                        {
                            wavAddr++;
                            wavCounter = gb.wavFreq;
                        }
                    }

                    if(noiseCounter > 0)
                        noiseCounter--;
                    if (noiseCounter == 0 && noiseLengthCounter != 0)
                    {
                        //Random
                        int feedback = ((shiftReg >> 1) ^ shiftReg) & 1;
                        shiftReg = ((shiftReg >> 1) & 0x3FFF) | (feedback << 14);
                        if (!gb.noiseShiftWidth)//7-bit
                            shiftReg = ((shiftReg >> 1) & 0x7FBF) | (feedback << 6);
                        noise = (shiftReg & 1) != 1;

                        noiseCounter = gb.noiseShiftFreq;
                    }
                }

                int square1OutVolume = dutyCycles[gb.square1DutyCycle][square1DutyCounter % 8] ? square1Volume : 0;
                int square2OutVolume = dutyCycles[gb.square2DutyCycle][square2DutyCounter % 8] ? square2Volume : 0;
                int wavOutVolume;
                if ((wavAddr & 1) == 0)
                    wavOutVolume = gb.hRam[0x30 | ((wavAddr >> 1) & 0xF)] >> 4;
                else
                    wavOutVolume = gb.hRam[0x30 | ((wavAddr >> 1) & 0xF)] & 0xF;
                wavOutVolume >>= gb.wavVolumeShift;
                int noiseOutVolume = noise ? noiseVolume : 0;

                int leftVolume = (gb.square1Left ? square1OutVolume : 0) + (gb.square2Left ? square2OutVolume : 0) + (gb.wavLeft ? wavOutVolume : 0) + (gb.noiseLeft ? noiseOutVolume : 0);
                int rightVolume = (gb.square1Right ? square1OutVolume : 0) + (gb.square2Right ? square2OutVolume : 0) + (gb.wavRight ? wavOutVolume : 0) + (gb.noiseRight ? noiseOutVolume : 0);

                leftVolume <<= 2;
                rightVolume <<= 2;

                if (!gb.soundEnabled)
                {
                    leftVolume = 0;
                    rightVolume = 0;
                }

                //leftVolume = square1Volume;
                //rightVolume = square2Volume;

                leftSampleTotal += leftVolume;
                rightSampleTotal += rightVolume;
                sampleCount++;
                sampleRateDivider--;
                if (sampleRateDivider <= 0)
                {
                    double sampleLeft = leftSampleTotal / (sampleCount * 1.0);
                    double sampleRight = rightSampleTotal / (sampleCount * 1.0);
                    rollingAveTotalLeft += sampleLeft;
                    rollingAveTotalRight += sampleRight;
                    rollingAveLeft.Enqueue(sampleLeft);
                    rollingAveRight.Enqueue(sampleRight);
                    if (rollingAveCount == rollingAveWindow)
                    {
                        rollingAveTotalLeft -= rollingAveLeft.Dequeue();
                        rollingAveTotalRight -= rollingAveRight.Dequeue();
                    }
                    else
                        rollingAveCount++;
                    sampleLeft -= rollingAveTotalLeft / (rollingAveCount * 1.0);
                    sampleRight -= rollingAveTotalRight / (rollingAveCount * 1.0);
                    output[outputPTR++] = (byte)(Math.Max(Math.Min(sampleLeft + 128, byte.MaxValue), byte.MinValue));
                    output[outputPTR++] = (byte)(Math.Max(Math.Min(sampleRight + 128, byte.MaxValue), byte.MinValue));
                    leftSampleTotal = 0;
                    rightSampleTotal = 0;
                    sampleCount = 0;
                    sampleRateDivider += sampleDivider;
                }
            }
        }
    }
}
