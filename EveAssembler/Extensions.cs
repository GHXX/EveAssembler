namespace EveAssembler;
public static class Extensions {
    public static string JoinBy<T>(this IEnumerable<T> src, string glue) {
        ArgumentNullException.ThrowIfNull(src);
        return string.Join(glue, src.Select(x => x?.ToString() ?? "NULL"));
    }
}
