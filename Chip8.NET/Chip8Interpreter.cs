using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8.NET
{
    public class Chip8Interpreter
    {
        // addresses 0x200 - 0xFFF
        private byte[] _ram = new byte[0x1000 - 0x200];

        // registers V0-VF, I
        private byte[] _v = new byte[0x10];
        private short _i;

        // internals
        private short _pc;
        private Stack<short> _sp = new Stack<short>();
        private bool[,] _lcd = new bool[64, 32];
        private bool[] _keys = new bool[0x10];
        private bool _running;
        private Random _random;

        public Chip8Interpreter()
        {
            Reset();
        }

        /// <summary>
        /// Reset the machine state.
        /// </summary>
        private void Reset()
        {
            Array.Clear(_ram, 0, _ram.Length);
            Array.Clear(_v, 0, _v.Length);
            _i = 0;
            _pc = 0x200;
            Array.Clear(_lcd, 0, _lcd.Length);
            Array.Clear(_keys, 0, _keys.Length);
            _running = false;
            _random = new Random();
        }

        /// <summary>
        /// Load a new program into memory.
        /// </summary>
        /// <param name="file">Binary file with Chip-8 data.</param>
        public void LoadProgram(string file)
        {
            Reset();
            byte[] data = File.ReadAllBytes(file);
            Array.Copy(data, _ram, _ram.Length);
        }

        /// <summary>
        /// Spins a thread to run the loaded program.
        /// </summary>
        public void Run()
        {
            _running = true;
            // Uh some threads or whatever
            while (_running)
            {
                Interpret();
            }
        }

        /// <summary>
        /// Executes the next instruction.
        /// </summary>
        public void Step()
        {
            Interpret();
        }
        
        /// <summary>
        /// Process the current instruction.
        /// </summary>
        private void Interpret()
        {
            // get instruction
            short op = BitConverter.ToInt16(_ram, _pc);
            Next();            

            // decode params
            byte code = (byte)(op & 0xF000 >> 12); // opcode
            short nnn = (short)(op & 0x0FFF); // address
            byte nn = (byte)(op & 0x00FF); // constant
            byte n = (byte)(op & 0x000F); // constant
            byte x = (byte)(op & 0x0F00 >> 8); // register #
            byte y = (byte)(op & 0x00F0 >> 4); // register #

            // run instruction
            switch (code)
            {
                case 0x0: if (nn == 0xE0) ClearLCD(); else
                          if (nn == 0xEE) Return(); break;
                case 0x1: Jump(nnn); break;
                case 0x2: Call(nnn); break;
                case 0x3: IfEqual(x, nn);  break;
                case 0x4: IfNotEqual(x, nn); break;
                case 0x5: IfEqualRegister(x, y); break;
                case 0x6: Assign(x, nn); break;
                case 0x7: Add(x, nn); break;
                case 0x8: Arithmetic(n, x, y); break;
                case 0x9: IfNotEqualRegister(x, y); break;
                case 0xA: SetAddress(nnn); break;
                case 0xB: JumpAddress(nnn); break;
                case 0xC: Rand(x, nn); break;
                case 0xD: DrawSprite(x, y, n); break;
                case 0xE: if (nn == 0x9E) KeyPressed(x); else
                          if (nn == 0xA1) KeyNotPressed(x); break;
                case 0xF: Misc(nn, x); break;
            }
        }

        /// <summary>
        /// Moves to the next instruction.
        /// </summary>
        private void Next()
        {
            _pc += 2;
        }

        /// <summary>
        /// Processes an arithmetic instruction.
        /// </summary>
        /// <param name="op">Opcode</param>
        /// <param name="x">Register V#</param>
        /// <param name="y">Register V#</param>
        private void Arithmetic(short op, byte x, byte y)
        {
            switch (op)
            {
                case 0x0: AssignRegister(x, y); break;
                case 0x1: Or(x, y); break;
                case 0x2: And(x, y); break;
                case 0x3: Xor(x, y); break;
                case 0x4: AddRegister(x, y); break;
                case 0x5: Subtract(x, y); break;
                case 0x6: ShiftRight(x); break;
                case 0x7: SubtractEx(x, y); break;
                case 0xE: ShiftLeft(x); break;
            }
        }

        /// <summary>
        /// Processes a miscellaneous instruction.
        /// </summary>
        /// <param name="op">Opcode</param>
        /// <param name="x">Register V#</param>
        private void Misc(short op, byte x)
        {
            switch (op)
            {
                case 0x07: break;
                case 0x0A: break;
                case 0x15: break;
                case 0x18: break;
                case 0x1E: break;
                case 0x29: break;
                case 0x33: break;
                case 0x55: break;
                case 0x65: break;
            }
        }

        /// <summary>
        /// Resets all pixels back to white.
        /// </summary>
        private void ClearLCD()
        {
            Array.Clear(_lcd, 0, _lcd.Length);
        }

        /// <summary>
        /// Returns from a subroutine.
        /// </summary>
        private void Return()
        {
            _pc = _sp.Pop();
        }

        /// <summary>
        /// Moves execution to another memory address.
        /// </summary>
        /// <param name="nnn">Address to jump to</param>
        private void Jump(short nnn)
        {
            _pc = nnn;
        }

        /// <summary>
        /// Calls a specified subroutine.
        /// </summary>
        /// <param name="nnn">Address of the subroutine</param>
        private void Call(short nnn)
        {
            _sp.Push(_pc);
            _pc = nnn;
        }

        /// <summary>
        /// Skips the next instruction if Vx == nn.
        /// </summary>
        /// <param name="x">Register V#</param>
        /// <param name="nn">Value to compare</param>
        private void IfEqual(byte x, byte nn)
        {
            if (_v[x] == nn)
            {
                Next();
            }
        }

        /// <summary>
        /// Skips the next instruction if Vx != nn.
        /// </summary>
        /// <param name="x">Register Vx</param>
        /// <param name="nn">Value to compare</param>
        private void IfNotEqual(byte x, byte nn)
        {
            if (_v[x] != nn)
            {
                Next();
            }
        }

        /// <summary>
        /// Skips the next instruction if Vx == Vy.
        /// </summary>
        /// <param name="x">Register Vx</param>
        /// <param name="y">Register Vy</param>
        private void IfEqualRegister(byte x, byte y)
        {
            if (_v[x] == _v[y])
            {
                Next();
            }
        }

        /// <summary>
        /// Assigns Vx = nn.
        /// </summary>
        /// <param name="x">Register V#</param>
        /// <param name="nn">Value to set</param>
        private void Assign(byte x, byte nn)
        {
            _v[x] = nn;
        }

        /// <summary>
        /// Adds Vx = Vx + nn, VF = carry.
        /// </summary>
        /// <param name="x">Register V#</param>
        /// <param name="nn">Value to add</param>
        private void Add(byte x, byte nn)
        {
            int result = _v[x] + nn;
            _v[x] += nn;
            _v[0xF] = (byte)((result > 0xFF) ? 1 : 0);
        }

        /// <summary>
        /// Assigns Vx = Vy.
        /// </summary>
        /// <param name="x">Register V# to set</param>
        /// <param name="y">Register V# to get</param>
        private void AssignRegister(byte x, byte y)
        {
            _v[x] = _v[y];
        }

        /// <summary>
        /// Performs a bitwise Vx = Vx OR Vy.
        /// </summary>
        /// <param name="x">Register V# that stores the result</param>
        /// <param name="y">Register V#</param>
        private void Or(byte x, byte y)
        {
            _v[x] &= _v[y];
        }

        /// <summary>
        /// Performs a bitwise Vx = Vx AND Vy.
        /// </summary>
        /// <param name="x">Register V# that stores the result</param>
        /// <param name="y">Register V#</param>
        private void And(byte x, byte y)
        {
            _v[x] |= _v[y];
        }

        /// <summary>
        /// Performs a bitwise Vx = Vx XOR Vy.
        /// </summary>
        /// <param name="x">Register V# that stores the result</param>
        /// <param name="y">Register V#</param>
        private void Xor(byte x, byte y)
        {
            _v[x] ^= _v[y];
        }

        /// <summary>
        /// AAdds Vx = Vx + Vy, VF = carry.
        /// </summary>
        /// <param name="x">Register V# to add to</param>
        /// <param name="y">Register V#</param>
        private void AddRegister(byte x, byte y)
        {
            int result = _v[x] + _v[y];
            _v[x] += _v[y];
            _v[0xF] = (byte)((result > 0xFF) ? 1 : 0);
        }

        /// <summary>
        /// Subtracts Vx = Vx - Vy, VF = borrow.
        /// </summary>
        /// <param name="x">Register V# to subtract from</param>
        /// <param name="y">Register V# </param>
        private void Subtract(byte x, byte y)
        {
            _v[0xF] = (byte)((_v[x] < _v[y]) ? 0 : 1);
            _v[x] -= _v[y];
        }

        /// <summary>
        /// Shifts Vx >> 1.
        /// </summary>
        /// <param name="x">Register V#</param>
        private void ShiftRight(byte x)
        {
            byte nn = _v[x];
            _v[x] >>= 1;
            _v[0xF] = (byte)(nn & 1);
        }

        /// <summary>
        /// Substracts Vx = Vy - Vx, VF = borrow.
        /// </summary>
        /// <param name="x">Register V#</param>
        /// <param name="y">Register V# to subtract from</param>
        private void SubtractEx(byte x, byte y)
        {
            _v[0xF] = (byte)((_v[y] < _v[x]) ? 0 : 1);
            byte nn = _v[y];
            nn -= _v[x];
            _v[x] = nn;
        }

        /// <summary>
        /// Shifts Vx << 1.
        /// </summary>
        /// <param name="x">Register V#</param>
        private void ShiftLeft(byte x)
        {
            byte nn = _v[x];
            _v[x] <<= 1;
            _v[0xF] = (byte)(nn & 0x8000 >> 15);
        }

        /// <summary>
        /// Skips the next instruction if two registers are not equal.
        /// </summary>
        /// <param name="x">Register V#</param>
        /// <param name="y">Register V#</param>
        private void IfNotEqualRegister(byte x, byte y)
        {
            if (_v[x] != _v[y])
            {
                Next();
            }
        }

        /// <summary>
        /// Assigns I = nnn.
        /// </summary>
        /// <param name="nnn">Address to assign</param>
        private void SetAddress(short nnn)
        {
            _i = nnn;
        }

        /// <summary>
        /// Jumps to the address NNN + V0
        /// </summary>
        /// <param name="nnn">Address to jump to</param>
        private void JumpAddress(short nnn)
        {
            _pc = (short)(nnn + _v[0x0]);
        }

        /// <summary>
        /// Generates a random byte and Vx = rand & nn;
        /// </summary>
        /// <param name="x">Register V#</param>
        /// <param name="nn">Maximum number</param>
        private void Rand(byte x, byte nn)
        {
            byte rand = (byte)_random.Next(0x00, 0x100);
            _v[x] = (byte)(rand & nn);
        }

        private void DrawSprite(byte x, byte y, byte n)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Skips the next instruction if Vx is pressed.
        /// </summary>
        /// <param name="x">Register V#</param>
        private void KeyPressed(byte x)
        {
            if (_keys[_v[x]])
            {
                Next();
            }
        }

        /// <summary>
        /// Skips the next instruction if Vx is not pressed.
        /// </summary>
        /// <param name="x">Register V#</param>
        private void KeyNotPressed(byte x)
        {
            if (!_keys[_v[x]])
            {
                Next();
            }
        }
    }
}
