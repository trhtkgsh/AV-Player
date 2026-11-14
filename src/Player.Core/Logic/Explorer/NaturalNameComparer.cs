namespace Player.Core.Logic.Explorer;

/// <summary>
/// Case-insensitive name ordering where runs of ASCII digits are compared by numeric value,
/// so "ep2" sorts before "ep10". Null sorts first. When two strings are equal in every other
/// respect, the one whose first differing digit run has fewer leading zeros sorts first
/// ("a1" before "a01"), which keeps the order total.
/// </summary>
public sealed class NaturalNameComparer : IComparer<string>
{
    public static NaturalNameComparer Instance { get; } = new();

    private NaturalNameComparer()
    {
    }

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var i = 0;
        var j = 0;
        var zeroTieBreak = 0;

        while (i < x.Length && j < y.Length)
        {
            if (IsAsciiDigit(x[i]) && IsAsciiDigit(y[j]))
            {
                var xEnd = DigitRunEnd(x, i);
                var yEnd = DigitRunEnd(y, j);

                ReadOnlySpan<char> xRun = x.AsSpan(i, xEnd - i);
                ReadOnlySpan<char> yRun = y.AsSpan(j, yEnd - j);

                ReadOnlySpan<char> xSignificant = StripLeadingZeros(xRun);
                ReadOnlySpan<char> ySignificant = StripLeadingZeros(yRun);

                var numeric = xSignificant.Length != ySignificant.Length
                    ? xSignificant.Length.CompareTo(ySignificant.Length)
                    : xSignificant.SequenceCompareTo(ySignificant);

                if (numeric != 0)
                {
                    return Math.Sign(numeric);
                }

                if (zeroTieBreak == 0)
                {
                    zeroTieBreak = (xRun.Length - xSignificant.Length)
                        .CompareTo(yRun.Length - ySignificant.Length);
                }

                i = xEnd;
                j = yEnd;
            }
            else
            {
                var cx = char.ToUpperInvariant(x[i]);
                var cy = char.ToUpperInvariant(y[j]);

                if (cx != cy)
                {
                    return cx < cy ? -1 : 1;
                }

                i++;
                j++;
            }
        }

        if (i < x.Length)
        {
            return 1;
        }

        if (j < y.Length)
        {
            return -1;
        }

        return Math.Sign(zeroTieBreak);
    }

    private static bool IsAsciiDigit(char c) => c >= '0' && c <= '9';

    private static int DigitRunEnd(string text, int start)
    {
        var end = start;
        while (end < text.Length && IsAsciiDigit(text[end]))
        {
            end++;
        }

        return end;
    }

    private static ReadOnlySpan<char> StripLeadingZeros(ReadOnlySpan<char> run)
    {
        var k = 0;
        while (k < run.Length && run[k] == '0')
        {
            k++;
        }

        return run.Slice(k);
    }
}
