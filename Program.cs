using Chip8Emu;
using Raylib_cs;

//string path = @"C:\Users\danie\Downloads\test_opcode (1).ch8";
//string path = @"C:\Users\danie\Downloads\1-chip8-logo.ch8";
string path = @"C:\Users\danie\Downloads\IBM Logo.ch8";

List<byte> program = new List<byte>();

var f = new FileStream(path, FileMode.Open);

while (f.Position < f.Length)
{
    program.Add((byte)f.ReadByte());
}




CPU cpu = new CPU(program.ToArray());
cpu.Run();
cpu.Draw();



Console.WriteLine("End.");