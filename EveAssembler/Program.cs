namespace EveAssembler;

internal class Program {
    private static void Main(string[] args) {
        Console.WriteLine("Hello, Assembler!");

        var asm = new Assembler();
        var code = """
            ldi r1, 123
            add r1, r1, r0
            jmpr -4
            """;
        var assembledLines = asm.Assemble(code);

        Console.WriteLine("Code:");

        // pad instruction names to width 4
        assembledLines = assembledLines.Select(x => (x.Item1.Split(' ', 2).Select((x, i) => i == 0 ? x.PadRight(4) : x).JoinBy(" "), x.Item2)).ToArray();
        var padSz = assembledLines.Max(x => x.Item1.Length) + 2;
        foreach (var line in assembledLines) {
            Console.WriteLine(line.Item1.PadRight(padSz) + "0x" + line.Item2.ToString("X4"));
        }
    }
}
