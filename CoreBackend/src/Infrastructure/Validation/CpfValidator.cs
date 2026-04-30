namespace CoreBackend.Infrastructure.Validation;

internal static class CpfValidator
{
    public static bool IsValid(string cpf)
    {
        var digits = cpf.Replace(".", "").Replace("-", "").Trim();

        if (digits.Length != 11 || !digits.All(char.IsDigit))
            return false;

        if (digits.Distinct().Count() == 1)
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (digits[i] - '0') * (10 - i);

        var remainder = sum % 11;
        var firstCheck = remainder < 2 ? 0 : 11 - remainder;

        if (digits[9] - '0' != firstCheck)
            return false;

        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += (digits[i] - '0') * (11 - i);

        remainder = sum % 11;
        var secondCheck = remainder < 2 ? 0 : 11 - remainder;

        return digits[10] - '0' == secondCheck;
    }
}
