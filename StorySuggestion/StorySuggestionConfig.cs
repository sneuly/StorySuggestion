using System.Reflection;

namespace StorySuggestion;

public static class StorySuggestionConfig
{
    public static readonly Dictionary<string, Dictionary<string, string>> GenderForms = new()
    {
        { "form1",    new() { { "mal", "мужчине" }, { "fem", "женщине" } } },
        { "form2",    new() { { "mal", "Ему"      }, { "fem", "Ей"       } } },
        { "readable", new() { { "not-ru", "не-русский" }, { "not-en", "не-английский" } } }
    };

    public static readonly Dictionary<string, Dictionary<string, string>> LangForms = new()
    {
        { "readable", new() { { "ru", "русский" }, { "en", "английский" } } },
        { "iso",      new() { { "ru", "ru-ru"   }, { "en", "en-us"      } } }
    };

    public const string Tale        = "tale";
    public const string Translation = "translation";
    public const string Modified    = "modified";
    public const string Mixed = "mixed";

    public const string TemplateForTale =
        "Сгенерируй рассказ о {gender:form1} {age} лет. {gender:form2} нравится мороженное.";

    public const string TemplateForTranslation =
        "Переведи текст с {fromLang:readable} на {toLang:readable}. Оставь в итогов тексте {count} слов.";

    public const string TemplateForModified =
        "Переведи текст с {fromLang:readable} на {toLang:readable}. Оставь в итогов тексте {count} слов. Должно быть 'Ему' - {gender:form2}. Год - {date}";
    
    public const string TemplateForMixed =
        "Переведи текст с {fromLang:readable}. Сгенерируй рассказ о {gender:form1} {age} лет. {gender:form2} нравится мороженное.";

    private static readonly Lazy<Dictionary<string, string>> TemplateMap = new(() =>
    {
        var type = typeof(StorySuggestionConfig);
        var constFields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                              .Where(f => f is { IsLiteral: true, IsInitOnly: false })
                              .ToList();

        var dict = new Dictionary<string, string>();

        foreach (var tplField in constFields.Where(f => f.Name.StartsWith("TemplateFor")))
        {
            var keyName   = tplField.Name.Replace("TemplateFor", "");
            var keyField  = constFields.FirstOrDefault(f => f.Name == keyName);
            if (keyField == null) continue;

            var key      = keyField.GetRawConstantValue()?.ToString();
            var template = tplField.GetRawConstantValue()?.ToString();
            if (key != null && template != null) dict[key] = template;
        }

        return dict;
    });

    public static string GetTemplate(string type)
        => TemplateMap.Value.TryGetValue(type, out var tpl)
               ? tpl
               : throw new ArgumentException($"Unknown type: {type}");

    public static Dictionary<string,
        Dictionary<string, Dictionary<string, string>>> GetAllLookups()
    {
        return typeof(StorySuggestionConfig)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.FieldType == typeof(Dictionary<string, Dictionary<string, string>>))
            .ToDictionary(
                f => f.Name,
                f => (Dictionary<string, Dictionary<string, string>>)f.GetValue(null)!);
    }
}
