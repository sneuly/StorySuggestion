using Newtonsoft.Json.Linq;

namespace StorySuggestion;

public interface IModelValidator
{
    IReadOnlyCollection<string> GetMissingMembers(Template template, JObject model);
}