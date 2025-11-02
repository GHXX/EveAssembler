namespace EveAssembler;

internal class Program {
#if DEBUG
    private const bool isRelease = false;
#else
    private const bool isRelease = true;
#endif

    private static void Main() {

        bool useArgs = Environment.GetCommandLineArgs().Length > 1 || isRelease;

        if (!useArgs) {
            Console.WriteLine("Running in test environment - not using arguments.");
            DebugMain();
        } else {
            ReleaseMain();
        }
    }

    private static void DebugMain() {
        var asm = new Assembler();
        var code = """
            ; r1 current address
            ldi r0, 0x2000; read start address
            lb r1, r0

            ; r2 max address to check (exclusive)
            ldi r2, 4095

            ; r3 read data
            ; r4 output
            ldi r4, 0

            ; ---- check cell ----
        checkLp:
            sb r1, r1 ; store r1 in r1
            lb r3, r1 ; r3=ram[r1]

            ; if data does not match
            sub r0, r3, r1
            jnz failed
        
            inci r1, 2
            sub r0, r2, r1
            jgz checkLp; jump if r1 < r2

            jmp ramOk
            ; ---- report result ----
        failed:
            ldi r4, 0xFFFF
            jmp end
        ramOk:
            ldi r4, 0x00FF
        end:hlt
        """;
        var assembledLines = asm.Assemble(code);

        Console.WriteLine("Code:");

        var padSz = assembledLines.Max(x => x.Item1.Length) + 2;
        foreach (var line in assembledLines) {
            Console.WriteLine(line.Item1.PadRight(padSz) + "0x" + line.Item2.ToString("X4"));
        }
    }

    private static void ReleaseMain() {
        Console.WriteLine("Reading args...");
        var args = CliArgs.Get();
        var code = File.ReadAllText(args.SourceFilePath);
        var asm = new Assembler();
        Console.WriteLine("Assembling...");
        var assembledLines = asm.Assemble(code);
        var outputBytes = assembledLines.SelectMany(x => new[] { (byte)(x.Item2 >> 8), (byte)(x.Item2 & 0xFF) }).ToArray();
        var outDir = new FileInfo(args.DestFilePath).DirectoryName ?? throw new Exception("should not be null");
        if (!Directory.Exists(outDir))
            Directory.CreateDirectory(outDir);

        File.WriteAllBytes(args.DestFilePath, outputBytes);
        Console.WriteLine($"Done, wrote {outputBytes.Length} bytes.");
    }
}
