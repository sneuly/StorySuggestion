using Newtonsoft.Json.Linq;

namespace StorySuggestion;

public interface ITemplateRenderer
{
    string Render(Template template, JObject model);
}