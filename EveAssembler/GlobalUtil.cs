global using static EveAssembler.GlobalUtil;
using EveAssembler.Types;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace EveAssembler;
public static class GlobalUtil {

    [DebuggerHidden]
    public static void Assert([DoesNotReturnIf(false)] bool condition, string? message = null) {
        if (!condition)
            throw new AssertionFailedException(string.IsNullOrWhiteSpace(message) ? "An assertion has failed!" : $"An assertion has failed: {message}");
    }

    [DebuggerHidden]
    public static void AssertImpl(bool a, bool b, string? message = null) => Assert(!a || b, message);
}
