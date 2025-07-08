namespace StorySuggestion;

public interface ILookupProvider
{
    bool TryResolve(string selector, string key, out string? value);
}