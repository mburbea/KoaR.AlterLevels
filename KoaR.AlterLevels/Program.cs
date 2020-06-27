using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace KoaR.AlterLevels
{
    static class Program
    {
        static List<int> GetAllIndices(ReadOnlySpan<byte> data, ReadOnlySpan<byte> sequence)
        {
            var results = new List<int>();
            int ix = data.IndexOf(sequence);
            int start = 0;

            while (ix != -1)
            {
                results.Add(start + ix - 3); // get the sim space
                start += ix + sequence.Length;
                ix = data.Slice(start).IndexOf(sequence);
            }
            return results;
        }

        static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = 
                Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var simSpaces = File.ReadLines("simspace.csv")
            .Skip(1)
            .ToDictionary(x => uint.Parse(x[..6], NumberStyles.HexNumber), x => x[7..]);

            if (args.Length < 2)
            {
                Console.WriteLine("Argument error, usage: path to save followed by level");
                return;
            }
            var path = args[0];
            if (!int.TryParse(args[1], out int level))
            {
                Console.WriteLine($"Could not parse {args[1]} as Byte");
                return;
            }

            var saveData = File.ReadAllBytes(path);
            ReadOnlySpan<byte> sequence = new byte[] { 0x00, 0x89, 0xFB, 0x40, 0x01, 0x03 };
            ReadOnlySpan<byte> allOnes = new byte[] { 0x1, 0x1, 0x1, 0x1 };
            var spaces = GetAllIndices(saveData, sequence);
            foreach (var spaceIx in spaces)
            {
                var id = BitConverter.ToUInt32(saveData, spaceIx);
                if (!simSpaces.TryGetValue(id, out var name))
                {
                    continue;
                }
                var dataLength = BitConverter.ToInt32(saveData, spaceIx + 9);
                Span<byte> span = saveData.AsSpan(spaceIx + 13, dataLength);
                var ixOfAllOnes = span.IndexOf(allOnes);
                if (ixOfAllOnes <= 11)
                {
                    continue;
                }
                var levelSpan = span.Slice(ixOfAllOnes - 12, 4);
                var oldLevel = MemoryMarshal.Read<int>(levelSpan);
                if ((uint)oldLevel < 255u)
                {
                    MemoryMarshal.Write(levelSpan, ref level);
                    Console.WriteLine($"Space:{name} oldLvl:{oldLevel} newLvl:{level}");
                }
            }
            Console.WriteLine($"Creating a backup: ${path}.bak");
            File.Copy(path, path + ".bak", overwrite: true);
            File.WriteAllBytes(path, saveData);
        }
    }
}
