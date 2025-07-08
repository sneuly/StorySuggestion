using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using StorySuggestion;

namespace StorySuggestionTests;

[TestFixture]
public class StorySuggestionServiceTests
{
    private StorySuggestionService _service = null!;

    #region ─── Setup ─────────────────────────────────────────────────────────────
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        var services = new ServiceCollection()
            .AddSingleton<ILookupProvider,     InMemoryLookupProvider>()
            .AddSingleton<ITemplateRepository, InMemoryTemplateRepository>()
            .AddSingleton<IModelValidator,     TemplateModelValidator>()
            .AddSingleton<ITemplateRenderer,   TemplateRenderer>()
            .AddSingleton<StorySuggestionService>();

        _service = services.BuildServiceProvider()
                           .GetRequiredService<StorySuggestionService>();
    }
    #endregion

    #region ─── Positive cases ────────────────────────────────────────────────────
    [Test, TestCaseSource(nameof(ValidCases))]
    public void Suggest_ShouldReturnExpectedText(TestCaseData data)
    {
        var text = _service.Suggest(data.Type, data.Model);
        Assert.That(text, Is.EqualTo(data.Expected));
    }
    #endregion

    #region ─── Negative cases ────────────────────────────────────────────────────
    [Test, TestCaseSource(nameof(InvalidCases))]
    public void Suggest_InvalidModel_ShouldThrow(TestCaseData data)
    {
        var ex = Assert.Throws<ArgumentException>(() => _service.Suggest(data.Type, data.Model));

        Assert.That(ex!.Message, Does.StartWith("Model lacks required members"));

        foreach (var field in data.ExpectedMissing!)
            Assert.That(ex.Message, Does.Contain(field));
    }
    #endregion

    #region ─── Test-case sources ────────────────────────────────────────────────
    private static IEnumerable<TestCaseData> ValidCases()
    {
        yield return new TestCaseData(
            StorySuggestionConfig.Tale,
            JObject.FromObject(new { gender = "mal", age = 18 }),
            "Сгенерируй рассказ о мужчине 18 лет. Ему нравится мороженное.");

        yield return new TestCaseData(
            StorySuggestionConfig.Tale,
            JObject.FromObject(new { gender = "fem", age = 30 }),
            "Сгенерируй рассказ о женщине 30 лет. Ей нравится мороженное.");

        yield return new TestCaseData(
            StorySuggestionConfig.Translation,
            JObject.FromObject(new { fromLang = "ru", toLang = "en", count = 50 }),
            "Переведи текст с русский на английский. Оставь в итогов тексте 50 слов.");

        yield return new TestCaseData(
            StorySuggestionConfig.Modified,
            JObject.FromObject(new
            {
                fromLang = "ru",
                toLang   = "en",
                gender   = "mal",
                date     = "28/11/2001",
                count    = 50
            }),
            "Переведи текст с русский на английский. Оставь в итогов тексте 50 слов. Должно быть 'Ему' - Ему. Год - 2001");
        
        yield return new TestCaseData(
            StorySuggestionConfig.Mixed,
            JObject.FromObject(new
            {
                fromLang = "not-ru",
                gender = "fem",
                age = 50
            }),
            "Переведи текст с не-русский. Сгенерируй рассказ о женщине 50 лет. Ей нравится мороженное.");
    }

    private static IEnumerable<TestCaseData> InvalidCases()
    {
        yield return new TestCaseData(
            StorySuggestionConfig.Modified,
            JObject.FromObject(new { gender = "mal", age = 18 }),
            ExpectedMissing: ["fromLang", "toLang", "count", "date"]);
    }
    #endregion

    #region ─── Helper DTO ───────────────────────────────────────────────────────
    public record TestCaseData(
        string  Type,
        JObject Model,
        string? Expected               = null,   // for valid cases
        string[]? ExpectedMissing      = null); // for invalid cases
    #endregion
}
