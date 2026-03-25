namespace CoreBackend.Exceptions;

public sealed class FieldsContentException : Exception
{
    // Fields deve ter um tipo que permita adicionar mensagens de erro a um campo sem que isso gere duplicidade
    private readonly Dictionary<string, List<string>> Fields = [];
    public Dictionary<string, List<string>> fields { get => Fields; }

    public FieldsContentException() { }

    public FieldsContentException(Dictionary<string, List<string>> fields)
    {
        Fields = fields;
    }

    public bool HasErrors()
    {
        return Fields.Count > 0;
    }

    public void AddError(string field, string message)
    {
        if (!Fields.TryGetValue(field, out var errors))
        {
            errors = new List<string>();
            Fields[field] = errors;
        }

        errors.Add(message);
    }

    public void AddErrors(string field, string[] messages)
    {
        foreach (var message in messages)
        {
            AddError(field, message);
        }
    }
}
