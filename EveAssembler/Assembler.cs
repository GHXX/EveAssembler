namespace EveAssembler;
internal class Assembler {
    // ushort (msb) opcode..operands (lsb)
    private static readonly Dictionary<string, Func<string[], ushort>> instructionMap = [];

    /*
     * Registers: 0-7, 0 is hardwired to zero
     * 
     */

    public Assembler() {
        void Add0Arg(string opName, ushort opcode) => instructionMap.Add(opName, args => {
            opcode <<= 11;
            Assert(args.Length == 0, $"Operation {opName} takes no arguments but received {args.JoinBy(",")}");
            return opcode;
        });

        void Add3RArg(string opName, ushort opcode) => instructionMap.Add(opName, args => {
            opcode <<= 11;
            Assert(args.Length == 3, $"Operation {opName} takes 3 arguments but received {args.JoinBy(",")}");
            return (ushort)(opcode | ParseRegister(args[2]) << 8 | ParseRegister(args[0]) << 4 | ParseRegister(args[1]));
        });

        void Add2RArg(string opName, ushort opcode) => instructionMap.Add(opName, args => {
            opcode <<= 11;
            Assert(args.Length == 2, $"Operation {opName} takes 2 arguments but received {args.JoinBy(",")}");
            return (ushort)(opcode | ParseRegister(args[1]) << 8 | ParseRegister(args[0]) << 4);
        });

        void Add1R1BImmArg(string opName, ushort opcode) => instructionMap.Add(opName, args => {
            opcode <<= 11;
            Assert(args.Length == 2, $"Operation {opName} takes 1 register argument and 1 immediate but received {args.JoinBy(",")}");
            return (ushort)(opcode | ParseRegister(args[0]) << 8 | ParseImmediate(args[1], true));
        });

        void Add1BImmArg(string opName, ushort opcode, bool isImmediateSigned) => instructionMap.Add(opName, args => {
            opcode <<= 11;
            Assert(args.Length == 1, $"Operation {opName} takes 1 immediate but received {args.JoinBy(",")}");
            return (ushort)(opcode | ParseImmediate(args[0], isImmediateSigned));
        });

        void Add3b1BImmArg(string opName, ushort opcode) => instructionMap.Add(opName, args => {
            opcode <<= 11;
            Assert(args.Length == 2, $"Operation {opName} takes 2 immediate but received {args.JoinBy(",")}");
            return (ushort)(opcode | ParseBitImmediate(args[0], 3) << 8 | ParseImmediate(args[1], true));
        });

        static byte ParseRegister(string input, bool allowZeroRegister = true) {
            Assert(input[0] == 'r');
            byte regId = byte.Parse(input[1..]);
            AssertImpl(!allowZeroRegister, regId != 0, "The zero register is invalid in this situation");
            return regId;
        }

        static byte ParseImmediate(string input, bool allowNegative) {
            AssertImpl(!allowNegative, input.Trim()[0] != '-');
            var parsed = int.Parse(input);
            if (parsed >= 0) {
                Assert(parsed <= byte.MaxValue, $"Argument {input} is too large for type byte!");
                return (byte)parsed;
            }
            return (byte)(uint)parsed;
        }

        static byte ParseBitImmediate(string input, int bitCount) {
            var parsed = byte.Parse(input);
            Assert(parsed >> bitCount == 0);
            return parsed;
        }

        Add0Arg("nop", 0);
        Add0Arg("hlt", 1);
        ushort nextOpcode = 2;
        foreach (var op in "add,sub,or,nor,and,nand,xor,xnor".Split(',')) {
            Add3RArg(op, nextOpcode++);
        }
        Add2RArg("lsh", 10);
        Add2RArg("rsh", 11);

        Add1R1BImmArg("ldi", 12);
        Add1BImmArg("jmp", 13, false);
        Add1R1BImmArg("adi", 14);
        Add1R1BImmArg("sui", 15);
        Add3b1BImmArg("brh", 16); // replace with pseudo instructions
        Add1BImmArg("jmpr", 17, true);
        Add1BImmArg("call", 18, false);
        Add0Arg("ret", 19);
    }

    public (string, ushort)[] Assemble(string code) {
        var assembledCode = new List<(string code, ushort bytes)>();
        foreach (var inputLine in code.Replace("\r\n", "\r").Replace('\r', '\n').Split('\n')) {
            string line = inputLine;
            while (true) {
                var newLine = line.Replace("  ", " ");
                if (newLine.Length == line.Length) break;
                line = newLine;
            }
            line = line.Trim().ToLowerInvariant();
            var splitted = line.Split(' ', 2);
            var opcode = splitted[0];
            var args = splitted[1].Split(',', StringSplitOptions.TrimEntries);
            var func = instructionMap[opcode];
            assembledCode.Add((line, func.Invoke(args)));
        }

        return assembledCode.ToArray();
    }
}
