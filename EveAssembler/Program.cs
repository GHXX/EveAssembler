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
            ldi r1, 46; current address
            ldi r2, 2047; max address to check (exclusive)
            ; ---- write data ----
        writeLp:; write address to each cell
            sb r1, r1; sb rS2, rS1 - takes value of RS2 and stores at address RS1

        ; check/step loop counter
            inci r1, 2
            sub r0, r2, r1
            jgz writeLp; jump if r1 < r2

            ; ---- check data ----
            ldi r1, 46; current address
        readLp:; read and check data from each cell
            lb r3, r1; read ram data

            ; if data does not match
            sub r0, r3, r1
            jnz failed

            ; check/step loop counter
            inci r1, 2
            sub r0, r2, r1
            jgz writeLp; jump if  r1 < r2            

            jmp ramOk

            ; ---- report result ----
        failed:
            ldi r3, 0xFF
            jmp end
        ramOk:
            ldi r3, 0x0F
            jmp end
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
