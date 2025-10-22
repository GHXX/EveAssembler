namespace EveAssembler;
internal class Assembler {
    private const int INSTRUCTION_SIZE_BYTES = 2;

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
            var parsed = ParsePotentialHexNumber(input);
            if (parsed >= 0) {
                Assert(parsed <= byte.MaxValue, $"Argument {input} is too large for type byte!");
                return (byte)parsed;
            }
            return (byte)(uint)parsed;
        }

        static int ParsePotentialHexNumber(string num) => num.StartsWith("0x") ? Convert.ToInt32(num[2..], 16) : int.Parse(num);

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
        Add1R1BImmArg("addi", 14);
        Add1R1BImmArg("sui", 15);
        Add3b1BImmArg("brh", 16); // replace with pseudo instructions
        Add1BImmArg("jmpr", 17, true);
        Add1BImmArg("call", 18, false);
        Add0Arg("ret", 19);
        Add1R1BImmArg("lui", 20);
        Add2RArg("sb", 21);
        Add2RArg("lb", 22);

        Assert(instructionMap.Values.Count == instructionMap.Values.Distinct().Count(), "duplicate opcodes appear to exist!");
    }


    private static string DeduplicateSpaces(string s) {
        s = s.Replace('\t', ' ');
        while (true) {
            var newLine = s.Replace("  ", " ");
            if (newLine.Length == s.Length) break;
            s = newLine;
        }
        return s;
    }

    private static string NormalizeLine(string s) {
        var lower = DeduplicateSpaces(s).Trim().ToLowerInvariant();

        if (lower.Contains(';')) {
            lower = lower[..lower.IndexOf(';')];
        }
        return lower;
    }

    public (string, ushort)[] Assemble(string code) {
        var assembledCode = new List<(string code, ushort bytes)>();
        var instructionLines = code.Replace("\r\n", "\r").Replace('\r', '\n').Split('\n').Select(NormalizeLine).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

        // Resolve label addresses
        var jumpLabels = new Dictionary<string, int>(); // lableName, positionAddress
        int currInstructionLine = 0;
        for (int i = 0; i < instructionLines.Length; i++) {
            string e;
            while (true) {
                e = instructionLines[i];
                var colonPos = e.IndexOf(":");
                if (colonPos == -1) break;
                var labelName = e[..colonPos].Trim();
                jumpLabels.Add(labelName, currInstructionLine * INSTRUCTION_SIZE_BYTES);
                instructionLines[i] = e[(colonPos + 1)..];
            }
            if (!string.IsNullOrWhiteSpace(e))
                currInstructionLine++;
        }

        instructionLines = instructionLines.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        // Assemble
        currInstructionLine = 0;
        var brhFunc = instructionMap["brh"];
        var jmprFunc = instructionMap["jmpr"];
        foreach (var inputLine in instructionLines) {
            string line = inputLine;
            var splitted = line.Split(' ', 2);
            var opcode = splitted[0];
            var args = splitted.Length <= 1 ? [] : splitted[1].Split(',', StringSplitOptions.TrimEntries);

            string GetJumpOffsetString(int jmpArgIdx) => ((-currInstructionLine + jumpLabels[args[jmpArgIdx]] - 1) * INSTRUCTION_SIZE_BYTES).ToString();
            ushort resultBytes;
            switch (opcode) {
                case "jmp": // operand is the target label
                    resultBytes = jmprFunc.Invoke([GetJumpOffsetString(0)]);
                    break;
                case "jgz":
                    resultBytes = brhFunc.Invoke(["0",GetJumpOffsetString(0)]);
                    break;
                case "jne":
                    resultBytes = brhFunc.Invoke(["1",GetJumpOffsetString(0)]);
                    break;
                default:
                    var func = instructionMap[opcode];
                    resultBytes = func.Invoke(args);
                    break;
            }
            assembledCode.Add((opcode.PadRight(4) + $" {args.JoinBy(", ")}", resultBytes));
            currInstructionLine++;
        }

        // prepend the labels to the asm dump
        var labelsForAddress = jumpLabels.Reverse().GroupBy(x => x.Value).ToDictionary(x => x.Key, x => x.Select(y => y.Key + ": ").JoinBy(""));
        var widestLabelPrefix = labelsForAddress.Max(x => x.Value.Length);
        for (int i = 0; i < assembledCode.Count; i++) {
            string labels = labelsForAddress.TryGetValue(i * INSTRUCTION_SIZE_BYTES, out var x) ? x : "";
            assembledCode[i] = (labels.PadRight(widestLabelPrefix) + assembledCode[i].code, assembledCode[i].bytes);
        }

        return assembledCode.ToArray();
    }
}
