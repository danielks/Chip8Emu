using Raylib_cs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Chip8Emu
{
    internal class CPU
    {
        public byte[] _memory;
        public byte[] _registers;

        Random rnd;

        private ushort VI;

        //first 512 (0x200) bytes are reserved for the original interpreter
        private const ushort _usableMemoryStartAddress = 0x200;


        //Special register. Delay timer.
        private byte DT;

        //Special register. Sound timer.
        private byte ST;

        //Program counter.
        private ushort PC;


        //stack pointer. Points to the topmost level of the stack
        //private byte SP;
        //Creio que nao vou precisar usar o registro SP, pois vou usar a classe Stack, que vai controlar isso.
        private Stack<ushort> _stack;

        private byte[,] _displayMemory;


        public CPU(byte[] program)
        {
            //4kB
            _memory = new byte[4096];


            rnd = new Random();
            program.CopyTo(_memory, _usableMemoryStartAddress);


            PC = _usableMemoryStartAddress;            

            _registers = new byte[16];

            _stack = new Stack<ushort>(16);

            VI = 0;

            ST = 0;
            DT = 0;

            _displayMemory = new byte[64, 32];

        
        }

        public void Run()
        {
            long cycleCount = 0;

            while (true)
            {
                //somente para testar um rom especifico. remover.
                if (cycleCount >= 39)
                {
                    //return;
                }

                if (cycleCount >= 10000)
                {
                    return;
                }


                if (PC >= _memory.Length - 1)
                {
                    return;
                }

                byte highByte = _memory[PC];
                byte lowByte = _memory[PC + 1];
                PC += 2;

                short data = (short)((short)(highByte << 8) | (short)lowByte);

                byte firstNibble = (byte)(data >> 12 & 0b00001111); //most significant 4 bits of the data.
                byte secondNibble = (byte)(data >> 8 & 0b00001111);
                byte thirdNibble = (byte)(data >> 4 & 0b00001111);
                byte fourthNibble = (byte)(data & 0b00001111);

                ushort nnn = (ushort)(data & 0b0000111111111111);

                
                ushort kk = lowByte;

                Instruction op = Decode(data);

                switch (op)
                {

                    case Instruction.CLS:
                        break;

                    case Instruction.RET:
                        PC = _stack.Pop();

                        break;

                    case Instruction.JP:
                        

                        PC = nnn;

                        break;

                    case Instruction.CALL:

                        //PC += 2;

                        this._stack.Push(PC);

                        PC = nnn;

                        break;

                    case Instruction.SE_Vx_byte:
                        if (_registers[secondNibble] == kk)
                        {
                            PC += 2;
                        }

                        break;


                    case Instruction.SNE_Vx_byte:
                        if (_registers[secondNibble] != kk)
                        {
                            PC += 2;
                        }

                        break;

                    case Instruction.SE_Vx_Vy:
                        if (_registers[secondNibble] == _registers[thirdNibble])
                        {
                            PC += 2;
                        }

                        break;

                    case Instruction.LD_Vx_byte:
                        _registers[secondNibble] = lowByte;
                        break;
                    case Instruction.ADD_Vx_byte:
                        _registers[secondNibble] += lowByte;
                        break;

                    case Instruction.SET:
                        _registers[secondNibble] = _registers[thirdNibble];

                        break;

                    case Instruction.BINARY_OR:
                        _registers[secondNibble] = (byte)(_registers[secondNibble] | _registers[thirdNibble]);

                        break;

                    case Instruction.BINARY_AND:
                        _registers[secondNibble] = (byte)(_registers[secondNibble] & _registers[thirdNibble]);

                        break;

                    case Instruction.LOGICAL_XOR:
                        _registers[secondNibble] = (byte)(_registers[secondNibble] ^ _registers[thirdNibble]);
                        break;

                    case Instruction.ADD:
                        _registers[secondNibble] = (byte)(_registers[secondNibble] + _registers[thirdNibble]);
                        break;

                    case Instruction.SUB_VX_VY:
                        _registers[secondNibble] = (byte)(_registers[secondNibble] + _registers[thirdNibble]);
                        break;

                    case Instruction.SUB_VY_VX:
                        _registers[secondNibble] = (byte)(_registers[thirdNibble] + _registers[secondNibble]);
                        break;

                    case Instruction.SHIFT_RIGHT:
                        _registers[secondNibble] = (byte)(_registers[thirdNibble] >> 1);
                        break;

                    case Instruction.SHIFT_LEFT:
                        _registers[secondNibble] = (byte)(_registers[thirdNibble] << 1);
                        break;
                    /*
    
    SUB_VX_VY = 8,        
    SUB_VY_VX = 8,
    SHIFT_RIGHT = 8,
    SHIFT_LEFT = 8,*/



                    case Instruction.LD_I_addr:
                        this.VI = nnn;
                        
                        break;

                    case Instruction.JUMP_WITH_OFFSET:
                        PC = (ushort)(_registers[0] + nnn);


                        break;

                    case Instruction.RANDOM:
                        byte rnd_num = (byte)rnd.Next(256);

                        _registers[secondNibble] = (byte)(rnd_num & lowByte);

                        break;

                    case Instruction.DRW_xyn:
                        byte x = secondNibble;
                        byte y = thirdNibble;
                        byte n = fourthNibble;

                        Execute_Instruction_DRW(x, y, n);

                        break;

                    case Instruction.SKIP_IF_KEY_PRESSED:
                        //if (key no _registers[x] esta pressinada)
                        //{
                        //    PC += 2;
                        //}

                        break;

                    case Instruction.SKIP_IF_KEY_NOT_PRESSED:
                        PC += 2;

                        break;



                    case Instruction.SET_VX_DT:
                        _registers[secondNibble] = DT;

                        break;

                    case Instruction.SET_DT_VX:
                        DT = _registers[secondNibble];
                        
                        break;

                    case Instruction.SET_ST_VX:
                        ST = _registers[secondNibble];

                        break;

                    case Instruction.ADD_TO_INDEX:
                        this.VI += _registers[secondNibble];

                        break;
                }

                //PC += 2;

                cycleCount++;

            }
        }

        private void Execute_Instruction_DRW(byte vx, byte vy, byte n)
        {
            byte startX = (byte)(_registers[vx] % 64);
            byte startY = (byte)(_registers[vy] % 32);

            ushort startAddr = VI;

            this._registers[0xF] = 0;

            for (ushort i = 0; i < n; i++)
            {
                //_displayMemory[x, y] = (byte)(_displayMemory[x, y] ^ _memory[i]);

                byte sprite = _memory[startAddr + i];

                //sprite = 3;

                var bits_temp = new BitArray(new[] { sprite });

                bool[] bits = new bool[8];
                bits[0] = bits_temp[7];
                bits[1] = bits_temp[6];
                bits[2] = bits_temp[5];
                bits[3] = bits_temp[4];
                bits[4] = bits_temp[3];
                bits[5] = bits_temp[2];
                bits[6] = bits_temp[1];
                bits[7] = bits_temp[0];

                for (int j = 0; j < 8; j++)
                
                
                {
                    //byte res = (byte)((sprite >> (8 - (i))) & 0b00000001);

                    byte x = (byte)(startX + j);
                    byte y = (byte)(startY + i);


                    if (x < 64 & y < 32)
                    {

                        byte res = (byte)(bits[j] ? 1 : 0);

                        byte old_value = _displayMemory[x, y];

                        byte new_value = (byte)(old_value ^ res);

                        _displayMemory[x, y] = new_value;

                        if (old_value == 1 && new_value == 0)
                        {
                            this._registers[0xF] = 1;
                        }
                    }
                }
            }
        }

        private Instruction Decode(short data)
        {
            if (data == 0x00E0)
            {
                return Instruction.CLS;
            }
            else if (data == 0x00EE)
            {
                return Instruction.RET;
            }
            else
            {
                
                byte firstNibble = (byte)(data >> 12 & 0b00001111); //most significant 4 bits of the data.
                byte secondNibble = (byte)(data >> 8 & 0b00001111);
                byte thirdNibble = (byte)(data >> 4 & 0b00001111);
                byte fourthNibble = (byte)(data & 0b00001111);

                byte lowByte = (byte)(data >> 8);

                if (firstNibble == 1)
                {
                    byte x = secondNibble;
                    short nn = (short)(((short)thirdNibble) << 8 | ((short)fourthNibble));

                    return Instruction.JP;


                }
                else if (firstNibble == 3)
                {
                    return Instruction.SE_Vx_byte;
                }
                else if (firstNibble == 4)
                {
                    return Instruction.SNE_Vx_byte;
                }
                else if (firstNibble == 5)
                {
                    return Instruction.SE_Vx_Vy;
                }
                else if (firstNibble == 6)
                {
                    return Instruction.LD_Vx_byte;
                }
                else if (firstNibble == 7)
                {
                    return Instruction.ADD_Vx_byte;
                }
                else if (firstNibble == 0xA)
                {
                    return Instruction.LD_I_addr;
                }
                else if (firstNibble == 0xB)
                {
                    return Instruction.JUMP_WITH_OFFSET;
                }
                else if (firstNibble == 0xC)
                {
                    return Instruction.RANDOM;
                }
                else if (firstNibble == 0xD)
                {
                    return Instruction.DRW_xyn;
                }
                else if (firstNibble == 0xE)
                {
                    if (lowByte == 0x9E)
                    {
                        return Instruction.SKIP_IF_KEY_PRESSED;
                    }
                    else if (lowByte == 0xA1)
                    {
                        return Instruction.SKIP_IF_KEY_NOT_PRESSED;
                    }
                }
                else if (firstNibble == 0xF)
                {
                    if (lowByte == 0x07)
                    {
                        return Instruction.SET_VX_DT;
                    }
                    else if (lowByte == 0x15)
                    {
                        return Instruction.SET_DT_VX;
                    }
                    else if (lowByte == 0x18)
                    {
                        return Instruction.SET_ST_VX;

                    }
                    else if (lowByte == 0x1E)
                    {
                        return Instruction.ADD_TO_INDEX;
                    }
                }
                else if (firstNibble == 8)
                {
                    if (fourthNibble == 0)
                    {
                        return Instruction.SET;
                    }
                    else if (fourthNibble == 1)
                    {
                        return Instruction.BINARY_OR;
                    }
                    else if (fourthNibble == 2)
                    {
                        return Instruction.BINARY_AND;

                    }
                    else if (fourthNibble == 3)
                    {
                        return Instruction.LOGICAL_XOR;
                    }
                    else if (fourthNibble == 4)
                    {
                        return Instruction.ADD;
                    }
                    else if (fourthNibble == 5)
                    {
                        return Instruction.SUB_VX_VY;
                    }
                    else if ((fourthNibble == 7))
                    {
                        return Instruction.SUB_VY_VX;
                    }
                    else if (fourthNibble == 6)
                    {
                        return Instruction.SHIFT_RIGHT;
                    }
                    else if (fourthNibble == 0xE)
                    {
                        return Instruction.SHIFT_LEFT;
                    }

                    
                }

                



                //throw new Exception(string.Format("Unknown instruction: {0}.", firstNibble));
                Console.WriteLine(string.Format("Unknown instruction: {0}.", firstNibble));

                return Instruction.Unknown;
            }


            //return Instruction.CLS; //TODO: REMOVE
            
        }

        public void Draw()
        {
            //Each pixel is 10 pixels
            int pixelWidth = 10;

            Raylib.InitWindow(64 * pixelWidth, 32 * pixelWidth, "Hello World");

            

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.White);

                //Raylib.DrawText("Hello, world!", 12, 12, 20, Color.Black);

                for (int y = 0; y < 32; y++)
                {

                    for (int x = 0; x < 64; x++)
                    {
                        Raylib.DrawRectangle(x*pixelWidth, y*pixelWidth, pixelWidth, pixelWidth, _displayMemory[x, y] == 0 ? Color.White : Color.Gray);
                    }

                    Console.WriteLine();
                }

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();

            for (int y = 0; y < 32; y++)
            {

                for (int x = 0; x < 64; x++)
                {
                    Console.Write(_displayMemory[x, y] == 0 ? ' ' : 'X');
                }

                Console.WriteLine();
            }
        }
    }

    public enum Instruction : byte
    {
        //Clear the display.
        CLS = 0x00E0,

        //Return from a subroutine.
        //The interpreter sets the program counter to the address at the top of the stack, then subtracts 1 from the stack pointer.
        RET = 0x00EE,


        //1nnn - JP addr
        //Jump to location nnn.
        //The interpreter sets the program counter to nnn.
        JP = 1,

        //2nnn - CALL addr
        //Call subroutine at nnn.
        //The interpreter increments the stack pointer, then puts the current PC on the top of the stack. The PC is then set to nnn.
        CALL = 2,

        //3xkk - SE Vx, byte
        //Skip next instruction if Vx = kk.
        //The interpreter compares register Vx to kk, and if they are equal, increments the program counter by 2.
        SE_Vx_byte = 3,

        SNE_Vx_byte = 4,

        SE_Vx_Vy = 5,

        

        LD_Vx_byte = 6,
        ADD_Vx_byte = 7,

        SET = 80,
        BINARY_OR = 81,
        BINARY_AND = 82,
        LOGICAL_XOR = 83,
        ADD = 84,
        SUB_VX_VY = 85,
        SUB_VY_VX = 87,
        SHIFT_RIGHT = 86,
        SHIFT_LEFT = 89,


        LD_I_addr = 0xA,
        JUMP_WITH_OFFSET = 0xB,
        RANDOM = 0xC,
        DRW_xyn = 0xD,


        SKIP_IF_KEY_PRESSED = 101, //nao bate, so pra representar
        SKIP_IF_KEY_NOT_PRESSED = 102, //idem


        

        SET_VX_DT = 200,
        SET_DT_VX = 201,
        SET_ST_VX = 203,

        ADD_TO_INDEX = 204,




        Unknown = 99
    }

}
