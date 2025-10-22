namespace EveAssembler;

internal class Program {
    private static void Main(string[] args) {
        Console.WriteLine("Hello, Assembler!");

        var asm = new Assembler();
        var code = """
            ldi r1, 0; current address
            lui r2, 0x8; max address to check (exclusive, 2048)
            ; ---- write data ----
        writeLp: ; write address to each cell
            sb r1, r1

        ; check/step loop counter
            addi r1, 1
            sub r0, r2, r1
            jgz writeLp ; jump if  r1 < r2

            ; ---- check data ----
            ldi r1, 0; current address
        readLp: ; read and check data from each cell
            lb r3, r1 ; read ram data

            ; if data does not match
            sub r0, r3, r1
            jne failed

            ; check/step loop counter
            addi r1, 1
            sub r0, r2, r1
            jgz writeLp ; jump if  r1 < r2            

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
}
