using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;

namespace Chip8.NET
{
    public class Chip8Interpreter
    {
        // hexadecimal characters
        private byte[] _hex = { 0xF0, 0x90, 0x90, 0x90, 0xF0,
                                0x20, 0x60, 0x20, 0x20, 0x70,
                                0xF0, 0x10, 0xF0, 0x80, 0xF0,
                                0xF0, 0x10, 0xF0, 0x10, 0xF0,
                                0x90, 0x90, 0xF0, 0x10, 0x10,
                                0xF0, 0x80, 0xF0, 0x10, 0xF0,
                                0xF0, 0x80, 0xF0, 0x90, 0xF0,
                                0xF0, 0x10, 0x20, 0x40, 0x40,
                                0xF0, 0x90, 0xF0, 0x90, 0xF0,
                                0xF0, 0x90, 0xF0, 0x10, 0xF0,
                                0xF0, 0x90, 0xF0, 0x90, 0x90,
                                0xE0, 0x90, 0xE0, 0x90, 0xE0,
                                0xF0, 0x80, 0x80, 0x80, 0xF0,
                                0xE0, 0x90, 0x90, 0x90, 0xE0,
                                0xF0, 0x80, 0xF0, 0x80, 0xF0,
                                0xF0, 0x80, 0xF0, 0x80, 0x80 };
        private static short HEX_START = 0x000;
        private static short HEX_SIZE = 5;

        // 0x200 - 0xFFF for program
        private byte[] _ram = new byte[0x1000];
        private static short PROGRAM_START = 0x200;

        // registers V0-VF, I
        private byte[] _v = new byte[0x10];
        private short _i;

        // internals
        private short _pc; // program counter
        private Stack<short> _sp = new Stack<short>(); // address stack
        private bool[] _keys = new bool[0x10]; // keys pressed
        private byte _kp; // current key pressed
        private byte _dt, _st; // delay and sound timer

        private bool _running;
        private Random _random;

        // screen pixels
        private static byte SCR_W = 64, SCR_H = 32;
        private ObservableCollection<bool> _screen = new ObservableCollection<bool>(Enumerable.Repeat(false, SCR_W * SCR_H));

        // databinding properties
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public ObservableCollection<bool> Screen { get { return _screen; } private set { _screen = value; } }

        private ListBox _debugger;
        private void Debug(string msg = "", [CallerMemberName] string caller = "")
        {
            _debugger.Items.Add("["+caller+"] "+msg);
        }

        public Chip8Interpreter(ListBox debugger)
        {
            _debugger = debugger;
            Reset();
        }

        /// <summary>
        /// Reset the machine state.
        /// </summary>
        private void Reset()        
        {
            Array.Clear(_ram, 0, _ram.Length);
            Array.Copy(_hex, _ram, _hex.Length);
            Array.Clear(_v, 0, _v.Length);
            _i = 0;
            _pc = 0x200;
            ClearScreen();
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
            Debug();
            Reset();
            byte[] data = File.ReadAllBytes(file);
            Array.Copy(data, 0, _ram, PROGRAM_START, data.Length);
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
            byte[] data = { _ram[_pc], _ram[_pc + 1] };
            Debug("pc = 0x" + _pc.ToString("x4"));
            Debug("data = 0x" + data[0].ToString("x2") + data[1].ToString("x2"));
            Next();            

            // decode params
            byte op = (byte)((data[0] & 0xF0) >> 4); // opcode - c000
            short nnn = BitConverter.ToInt16(new byte[] { data[1], (byte)(data[0] & 0x0F) }, 0); // address - 0nnn
            byte nn = (byte)(data[1]); // constant - 00nn
            byte n = (byte)(data[1] & 0x0F); // constant - 000n
            byte x = (byte)(data[0] & 0x0F); // register # - 0x00
            byte y = (byte)((data[1] & 0xF0) >> 4); // register # - 00y0

            /*Debug("op = 0x" + op.ToString("x"));
            Debug("nnn = 0x" + nnn.ToString("x3"));
            Debug("nn = 0x" + nn.ToString("x2"));
            Debug("n = 0x" + n.ToString("x"));
            Debug("x = 0x" + x.ToString("x"));
            Debug("y = 0x" + y.ToString("x"));*/

            // run instruction
            switch (op)
            {
                case 0x0: if (nn == 0xE0) ClearScreen(); else
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
                case 0x07: SetDelayTimer(x); break;
                case 0x0A: GetKeyPressed(x); break;
                case 0x15: SetDelayTimer(x); break;
                case 0x18: SetSoundTimer(x); break;
                case 0x1E: AddAddress(x); break;
                case 0x29: HexSprite(x); break;
                case 0x33: BCD(x);  break;
                case 0x55: SaveRegisters(); break;
                case 0x65: LoadRegisters(); break;
            }
        }

        /// <summary>
        /// Resets all pixels back to white.
        /// </summary>
        private void ClearScreen()
        {
            Debug();
            for (int i = 0; i < SCR_W * SCR_H; i++)
                Screen[i] = false;
        }

        /// <summary>
        /// Returns from a subroutine.
        /// </summary>
        private void Return()
        {
            Debug();
            _pc = _sp.Pop();
        }

        /// <summary>
        /// Moves execution to another memory address.
        /// </summary>
        /// <param name="nnn">Address to jump to</param>
        private void Jump(short nnn)
        {
            Debug("nnn = 0x" + nnn.ToString("x4"));
            _pc = nnn;
        }

        /// <summary>
        /// Calls a specified subroutine.
        /// </summary>
        /// <param name="nnn">Address of the subroutine</param>
        private void Call(short nnn)
        {
            Debug("nnn = 0x" + nnn.ToString("x4"));
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
            Debug("x = 0x" + x.ToString("x"));
            Debug("nn = 0x" + nn.ToString("x2"));
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
            Debug("x = 0x" + x.ToString("x"));
            Debug("nn = 0x" + nn.ToString("x2"));
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
            Debug("x = 0x" + x.ToString("x"));
            Debug("y = 0x" + y.ToString("x"));
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
            Debug("x = 0x" + x.ToString("x"));
            Debug("nn = 0x" + nn.ToString("x2"));
            _v[x] = nn;
        }

        /// <summary>
        /// Adds Vx = Vx + nn, VF = carry.
        /// </summary>
        /// <param name="x">Register V#</param>
        /// <param name="nn">Value to add</param>
        private void Add(byte x, byte nn)
        {
            Debug("x = 0x" + x.ToString("x"));
            Debug("nn = 0x" + nn.ToString("x2"));
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
            Debug("x = 0x" + x.ToString("x"));
            Debug("y = 0x" + y.ToString("x"));
            _v[x] = _v[y];
        }

        /// <summary>
        /// Performs a bitwise Vx = Vx OR Vy.
        /// </summary>
        /// <param name="x">Register V# that stores the result</param>
        /// <param name="y">Register V#</param>
        private void Or(byte x, byte y)
        {
            Debug("x = 0x" + x.ToString("x"));
            Debug("y = 0x" + y.ToString("x"));
            _v[x] &= _v[y];
        }

        /// <summary>
        /// Performs a bitwise Vx = Vx AND Vy.
        /// </summary>
        /// <param name="x">Register V# that stores the result</param>
        /// <param name="y">Register V#</param>
        private void And(byte x, byte y)
        {
            Debug("x = 0x" + x.ToString("x"));
            Debug("y = 0x" + y.ToString("x"));
            _v[x] |= _v[y];
        }

        /// <summary>
        /// Performs a bitwise Vx = Vx XOR Vy.
        /// </summary>
        /// <param name="x">Register V# that stores the result</param>
        /// <param name="y">Register V#</param>
        private void Xor(byte x, byte y)
        {
            Debug("x = 0x" + x.ToString("x"));
            Debug("y = 0x" + y.ToString("x"));
            _v[x] ^= _v[y];
        }

        /// <summary>
        /// Adds Vx = Vx + Vy, VF = carry.
        /// </summary>
        /// <param name="x">Register V# to add to</param>
        /// <param name="y">Register V#</param>
        private void AddRegister(byte x, byte y)
        {
            Debug("x = 0x" + x.ToString("x"));
            Debug("y = 0x" + y.ToString("x"));
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
            Debug("x = 0x" + x.ToString("x"));
            Debug("y = 0x" + y.ToString("x"));
            _v[0xF] = (byte)((_v[x] < _v[y]) ? 0 : 1);
            _v[x] -= _v[y];
        }

        /// <summary>
        /// Shifts Vx >> 1.
        /// </summary>
        /// <param name="x">Register V#</param>
        private void ShiftRight(byte x)
        {
            Debug("x = 0x" + x.ToString("x"));
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
            Debug("x = 0x" + x.ToString("x"));
            Debug("y = 0x" + y.ToString("x"));
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
            Debug("x = 0x" + x.ToString("x"));
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
            Debug("x = 0x" + x.ToString("x"));
            Debug("y = 0x" + y.ToString("x"));
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
            Debug("nnn = 0x" + nnn.ToString("x4"));
            _i = nnn;
        }

        /// <summary>
        /// Jumps to the address NNN + V0
        /// </summary>
        /// <param name="nnn">Address to jump to</param>
        private void JumpAddress(short nnn)
        {
            Debug("nnn = 0x" + nnn.ToString("x4"));
            _pc = (short)(nnn + _v[0x0]);
        }

        /// <summary>
        /// Generates a random byte and Vx = rand & nn;
        /// </summary>
        /// <param name="x">Register V#</param>
        /// <param name="nn">Maximum number</param>
        private void Rand(byte x, byte nn)
        {
            Debug("x = 0x" + x.ToString("x"));
            Debug("nn = 0x" + nn.ToString("x2"));
            byte rand = (byte)_random.Next(0x00, 0x100);
            _v[x] = (byte)(rand & nn);
        }

        /// <summary>
        /// Draws a sprite on the screen at Vx,Vy coordinates.
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="n">Size of the sprite</param>
        private void DrawSprite(byte x, byte y, byte n)
        {
            Debug("x = 0x" + x.ToString("x"));
            Debug("y = 0x" + y.ToString("x"));
            Debug("n = 0x" + n.ToString("x"));
            _v[0xF] = 0;
            byte spriteX = _v[x], spriteY = _v[y];
            for (int b = 0; b < n; b++)
            {
                byte row = _ram[_i + b];
                for (int p = 7; p >= 0; p--)
                {
                    bool pixel = (row & 1) == 1;
                    int pos = (spriteX + p) % SCR_W + ((spriteY + b) % SCR_H) * SCR_W;
                    if (Screen[pos])
                        _v[0xF] = 1;
                    Screen[pos] ^= pixel;
                    row >>= 1;
                }
            }
        }

        /// <summary>
        /// Skips the next instruction if Vx is pressed.
        /// </summary>
        /// <param name="x">Register V#</param>
        private void KeyPressed(byte x)
        {
            Debug("x = 0x" + x.ToString("x"));
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
            Debug("x = 0x" + x.ToString("x"));
            if (!_keys[_v[x]])
            {
                Next();
            }
        }

        /// <summary>
        /// Stores the delay timer in Vx.
        /// </summary>
        /// <param name="x">Register V#</param>
        private void GetDelayTimer(byte x)
        {
            Debug("x = 0x" + x.ToString("x"));
            _v[x] = _dt;
        }

        /// <summary>
        /// Waits until a key is pressed and stores it in Vx.
        /// </summary>
        /// <param name="x">Register V#</param>
        private void GetKeyPressed(byte x)
        {
            Debug("x = 0x" + x.ToString("x"));
            //while (_kp == 0xFF) { }
            _v[x] = _kp;
            _kp = 0xFF;
        }

        /// <summary>
        /// Changes the delay timer to Vx.
        /// </summary>
        /// <param name="x">Register V#</param>
        private void SetDelayTimer(byte x)
        {
            Debug("x = 0x" + x.ToString("x"));
            _dt = _v[x];
        }

        /// <summary>
        /// Changes the sound timer to Vx.
        /// </summary>
        /// <param name="x">Register V#</param>
        private void SetSoundTimer(byte x)
        {
            Debug("x = 0x" + x.ToString("x"));
            _st = _v[x];
        }

        /// <summary>
        /// Adds the address in Vx to I.
        /// </summary>
        /// <param name="x">Register V#</param>
        private void AddAddress(byte x)
        {
            Debug("x = 0x" + x.ToString("x"));
            _i += _v[x];
        }

        /// <summary>
        /// Gets the address for the hexadecimal sprite for number Vx.
        /// </summary>
        /// <param name="x">Register Vx</param>
        private void HexSprite(byte x)
        {
            Debug("x = 0x" + x.ToString("x"));
            _i = (short)(HEX_START + _v[x] * HEX_SIZE);
        }

        /// <summary>
        /// Stores in I a Binary-Coded-Decimal of Vx.
        /// </summary>
        /// <param name="x">Register Vx</param>
        private void BCD(byte x)
        {
            Debug("x = 0x" + x.ToString("x"));
            for (byte i = 2, value = _v[x]; value > 0; value /= 10, i--)
            {
                _ram[_i+i] = (byte)(value % 10);
            }
        }

        /// <summary>
        /// Saves all registers to the memory at I.
        /// </summary>
        private void SaveRegisters()
        {
            Debug();
            Array.Copy(_v, 0, _ram, _i, _v.Length);
        }

        /// <summary>
        /// Loads all registers from the memory at I.
        /// </summary>
        private void LoadRegisters()
        {
            Debug();
            Array.Copy(_ram, _i, _v, 0, _v.Length);
        }
    }
}
