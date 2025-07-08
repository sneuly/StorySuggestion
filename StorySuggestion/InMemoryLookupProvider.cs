namespace StorySuggestion;

public sealed class InMemoryLookupProvider : ILookupProvider
{
    private readonly Dictionary<
        string,
        Dictionary<string, Dictionary<string, string>>> _lookups
        = StorySuggestionConfig.GetAllLookups();


    public bool TryResolve(string selector, string key, out string? value)
    {
        foreach (var dictGroup in _lookups.Values)
        {
            if (dictGroup.TryGetValue(selector, out var dict) &&
                dict.TryGetValue(key, out value))
                return true;
        }

        value = null;
        return false;
    }

    public Dictionary<
        string,
        Dictionary<string, Dictionary<string, string>>> AllGroups => _lookups;
}