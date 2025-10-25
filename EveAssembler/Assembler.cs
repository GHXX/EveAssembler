namespace EveAssembler;
internal class Assembler {
    private const int INSTRUCTION_SIZE_BYTES = 2;

    // ushort (msb) opcode..operands (lsb)
    private static readonly Dictionary<string, Func<string[], ushort>> instructionMap = [];

    private static readonly Dictionary<string, Func<string[], string[]>> pseudoInstructionMap = []; // args -> lines

    /*
     * Registers: 0-7, 0 is hardwired to zero
     * 
     */

    public Assembler() {
        void Add0Arg(string opName, ushort opcode) => instructionMap.Add(opName, args => {
            Assert(args.Length == 0, $"Operation {opName} takes no arguments but received {args.JoinBy(",")}");
            return (ushort)(opcode << 11);
        });

        void Add3RArg(string opName, ushort opcode) => instructionMap.Add(opName, args => {
            Assert(args.Length == 3, $"Operation {opName} takes 3 arguments but received {args.JoinBy(",")}");
            return (ushort)(opcode << 11 | ParseRegister(args[2]) << 8 | ParseRegister(args[0]) << 4 | ParseRegister(args[1]));
        });

        void Add2RArg(string opName, ushort opcode) => instructionMap.Add(opName, args => {
            Assert(args.Length == 2, $"Operation {opName} takes 2 arguments but received {args.JoinBy(",")}");
            return (ushort)(opcode << 11 | ParseRegister(args[1]) << 8 | ParseRegister(args[0]) << 4);
        });

        void Add1R1BImmArg(string opName, ushort opcode) {
            instructionMap.Add(opName, args => {
                Assert(args.Length == 2, $"Operation {opName} takes 1 register argument and 1 immediate but received {args.JoinBy(",")}");
                return (ushort)(opcode << 11 | ParseRegister(args[0]) << 8 | ParseImmediate(args[1], true));
            });
        }

        void Add1BImmArg(string opName, ushort opcode, bool isImmediateSigned) => instructionMap.Add(opName, args => {
            Assert(args.Length == 1, $"Operation {opName} takes 1 immediate but received {args.JoinBy(",")}");
            return (ushort)(opcode << 11 | ParseImmediate(args[0], isImmediateSigned));
        });

        void Add3b1BImmArg(string opName, ushort opcode) => instructionMap.Add(opName, args => {
            Assert(args.Length == 2, $"Operation {opName} takes 2 immediate but received {args.JoinBy(",")}");
            return (ushort)(opcode << 11 | ParseBitImmediate(args[0], 3) << 8 | ParseImmediate(args[1], true));
        });

        static byte ParseRegister(string input, bool allowZeroRegister = true) {
            Assert(input[0] == 'r');
            byte regId = byte.Parse(input[1..]);
            AssertImpl(!allowZeroRegister, regId != 0, "The zero register is invalid in this situation");
            Assert(regId <= 7 && regId >= 0);
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
        foreach (var op in "add,sub,or,and,xor,lsh,rsh".Split(',')) {
            Add3RArg(op, nextOpcode++);
        }

        Add1R1BImmArg("lui", 10);
        Add1R1BImmArg("inci", 11);
        Add1R1BImmArg("sui", 12);
        Add3b1BImmArg("brh", 13); // replace with pseudo instructions
        Add1BImmArg("call", 15, false);
        Add0Arg("ret", 16);
        Add2RArg("sw", 17);
        Add2RArg("lw", 18);

        void AddPseudoInstruction(string instructionName, Func<string[], string[]> argsToInstructions) {
            pseudoInstructionMap.Add(instructionName, argsToInstructions);
        }

        AddPseudoInstruction("ldi", args => {
            Assert(args.Length == 2);
            var reg = ParseRegister(args[0]);
            var num = ParsePotentialHexNumber(args[1]);
            Assert(short.MinValue <= num && num <= ushort.MaxValue);
            byte upper = (byte)(num >> 8);
            byte lower = (byte)(num & 0xFF);

            // zero the register, then lui upper, then inci lower
            var r = $"r{reg}";
            List<string> rv = [$"lui {r}, 0x{upper:X2}"];
            if (lower != 0)
                rv.Add($"inci {r}, 0x{lower:X2}");
            return rv.ToArray();
        });

        var union = instructionMap.Select(x => x.Key).Concat(pseudoInstructionMap.Select(x => x.Key)).ToArray();
        Assert(union.Length == union.Distinct().Count(), "duplicate opcodes appear to exist!");
    }

    private static void AssembleInstruction(Dictionary<string, int> jumpLabels, string line, List<(string code, ushort bytes)> resultAssembledCode, AssemblerStage stage) {
        var currInstructionLine = resultAssembledCode.Count;

        var splitted = line.Split(' ', 2);
        var opcode = splitted[0];
        var args = splitted.Length <= 1 ? [] : splitted[1].Split(',', StringSplitOptions.TrimEntries);


        string GetJumpOffsetString(int jmpArgIdx) => stage == AssemblerStage.ResolveJumpLabels ? "0" : ((jumpLabels[args[jmpArgIdx]] - (currInstructionLine + 1) * INSTRUCTION_SIZE_BYTES).ToString()); // offset seems wrong?

        if (pseudoInstructionMap.TryGetValue(opcode, out var map)) {
            foreach (var ins in map.Invoke(args)) {
                AssembleInstruction(jumpLabels, ins, resultAssembledCode, stage);
            }
            return; // already done processing it
        }


        var brhFunc = instructionMap["brh"];
        ushort resultBytes;
        switch (opcode) {
            case "jmp": // operand is the target label
                resultBytes = brhFunc.Invoke(["7", GetJumpOffsetString(0)]);
                break;
            case "jgz":
                resultBytes = brhFunc.Invoke(["2", GetJumpOffsetString(0)]);
                break;
            case "jnz":
                resultBytes = brhFunc.Invoke(["4", GetJumpOffsetString(0)]);
                break;
            default:
                var func = instructionMap[opcode];
                resultBytes = func.Invoke(args);
                break;
        }
        resultAssembledCode.Add((opcode.PadRight(4) + $" {args.JoinBy(", ")}", resultBytes));
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
        for (int i = 0; i < instructionLines.Length; i++) {
            string e;
            while (true) {
                e = instructionLines[i];
                var colonPos = e.IndexOf(":");
                if (colonPos == -1) break;
                var labelName = e[..colonPos].Trim();
                jumpLabels.Add(labelName, assembledCode.Count * INSTRUCTION_SIZE_BYTES);
                instructionLines[i] = e[(colonPos + 1)..];
            }
            if (!string.IsNullOrWhiteSpace(e)) {
                AssembleInstruction(jumpLabels, e, assembledCode, AssemblerStage.ResolveJumpLabels);
            }
        }
        assembledCode.Clear();

        instructionLines = instructionLines.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        // Assemble
        foreach (var inputLine in instructionLines) {
            AssembleInstruction(jumpLabels, inputLine, assembledCode, AssemblerStage.Emit);
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

    private enum AssemblerStage {
        ResolveJumpLabels,
        Emit
    }
}
