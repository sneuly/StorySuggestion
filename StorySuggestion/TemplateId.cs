namespace StorySuggestion;

public readonly record struct TemplateId(string Value)
{
    public override string ToString() => Value;
}