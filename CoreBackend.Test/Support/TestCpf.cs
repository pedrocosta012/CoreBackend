namespace CoreBackend.Test.Support;

internal static class TestCpf
{
    private static readonly Random Rng = new();

    public static string Generate()
    {
        var digits = new int[11];
        for (var i = 0; i < 9; i++)
            digits[i] = Rng.Next(0, 10);

        // Avoid all-same-digit CPFs
        if (digits.Take(9).Distinct().Count() == 1)
            digits[8] = (digits[8] + 1) % 10;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += digits[i] * (10 - i);
        var remainder = sum % 11;
        digits[9] = remainder < 2 ? 0 : 11 - remainder;

        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += digits[i] * (11 - i);
        remainder = sum % 11;
        digits[10] = remainder < 2 ? 0 : 11 - remainder;

        return string.Concat(digits);
    }
}
