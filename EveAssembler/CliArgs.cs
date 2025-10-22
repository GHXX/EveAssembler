namespace EveAssembler;
internal record class CliArgs {

    public string SourceFilePath { get; private set; }
    public string DestFilePath { get; private set; }

    public CliArgs() { }

    public static CliArgs Get() {
        var args = Environment.GetCommandLineArgs()[1..];
        var rv = new CliArgs();
        var dict = new Dictionary<string, string>();
        string? currKey = null;
        foreach (var arg in args) {
            if (currKey == null) {
                Assert(arg.StartsWith("--"));
                currKey = arg;
            } else {
                Assert(!arg.StartsWith("--"));
                dict.Add(currKey[2..], arg);
                currKey = null;
            }
        }

        T ReadOptionalArg<T>(string name, T defaultValue) where T : IParsable<T> {
            return dict.TryGetValue(name, out var value) ? T.Parse(value, null) : defaultValue;
        }

        T ReadRequiredArg<T>(string name) where T : IParsable<T> {
            Assert(dict.TryGetValue(name, out var value));
            return T.Parse(value, null);
        }

        rv.SourceFilePath = ReadRequiredArg<string>("sourceFile");
        rv.DestFilePath = ReadRequiredArg<string>("destFile");
        return rv;
    }
}
