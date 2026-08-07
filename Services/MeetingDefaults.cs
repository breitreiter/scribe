using System.Text.RegularExpressions;

namespace Scribe.Services;

/// <summary>
/// Best guesses for the three values a human confirms: date, title, purpose.
/// Pure and testable — the prompting itself lives in Program, so console I/O stays
/// out of the service layer.
/// </summary>
public static partial class MeetingDefaults
{
    private static readonly string[] MediaExtensions =
    [
        ".m4a", ".mp3", ".wav", ".flac", ".ogg", ".oga", ".aac", ".wma", ".opus",
        ".mp4", ".m4v", ".mov", ".mkv", ".webm", ".avi"
    ];

    // Zoom: GMT20260714-140012_Recording.m4a
    [GeneratedRegex(@"(?:GMT)?(20\d{2})(\d{2})(\d{2})[-_]\d{4,6}")]
    private static partial Regex ZoomStamp();

    // Anything carrying an ISO date: 2026-07-14_standup.m4a, or the directory name
    [GeneratedRegex(@"(20\d{2})-(\d{2})-(\d{2})")]
    private static partial Regex IsoDate();

    // Bare yyyyMMdd, checked last because it is the easiest to match by accident
    [GeneratedRegex(@"(?<![\d])(20\d{2})(\d{2})(\d{2})(?![\d])")]
    private static partial Regex CompactDate();

    /// <summary>
    /// The date the meeting happened, guessed from the recording's filename, then the
    /// directory name, then the media file's timestamp. Never silently today's date:
    /// the processing date is the one value guaranteed to be wrong on a reprocess, and
    /// it corrupts the field retrieval filters on hardest.
    /// </summary>
    public static string GuessDate(string directory, string? rawJsonPath = null)
    {
        var mediaFile = FindMediaFile(directory);

        foreach (var candidate in new[] { mediaFile, Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar)) })
        {
            if (string.IsNullOrEmpty(candidate)) continue;
            if (TryParseDate(candidate, out var fromName)) return fromName;
        }

        // Fall back to when the recording was written, which is at least near the meeting.
        var timestampSource = mediaFile != null
            ? Path.Combine(directory, mediaFile)
            : rawJsonPath;

        if (timestampSource != null && File.Exists(timestampSource))
            return File.GetLastWriteTime(timestampSource).ToString("yyyy-MM-dd");

        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    public static bool TryParseDate(string text, out string isoDate)
    {
        foreach (var regex in new[] { ZoomStamp(), IsoDate(), CompactDate() })
        {
            var match = regex.Match(text);
            if (!match.Success) continue;

            var year = int.Parse(match.Groups[1].Value);
            var month = int.Parse(match.Groups[2].Value);
            var day = int.Parse(match.Groups[3].Value);

            if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) continue;

            isoDate = $"{year:D4}-{month:D2}-{day:D2}";
            return true;
        }

        isoDate = string.Empty;
        return false;
    }

    public static string? FindMediaFile(string directory) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory)
                .Select(Path.GetFileName)
                .Where(name => name != null && MediaExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
                .OrderBy(name => name, StringComparer.Ordinal)
                .FirstOrDefault()
            : null;

    /// <summary>
    /// A title from the AI one-liner. A one-liner is a sentence and a title is not, so
    /// it is trimmed to something that reads as a name and survives being a filename —
    /// the raw sentence produced a 94-character slug.
    /// </summary>
    public static string TitleFrom(string? oneLiner, string fallbackDate)
    {
        if (string.IsNullOrWhiteSpace(oneLiner))
            return $"{fallbackDate} meeting";

        var title = oneLiner.Trim().TrimEnd('.', '!', '?').Trim();

        // Cut at the first clause boundary if there is one early enough to still be informative.
        var boundary = title.IndexOfAny([',', ';', ':']);
        if (boundary >= 20) title = title[..boundary];

        const int maxLength = 60;
        if (title.Length > maxLength)
        {
            var lastSpace = title.LastIndexOf(' ', maxLength);
            title = title[..(lastSpace > 20 ? lastSpace : maxLength)];
            title = TrimDanglingWords(title);
        }

        return title.TrimEnd(',', ';', ':', ' ');
    }

    /// <summary>
    /// Words no title should end on. Cutting a sentence at a word boundary is not enough:
    /// "…and agreed on a" is a word boundary and still reads as broken text.
    /// </summary>
    private static readonly HashSet<string> DanglingWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "but", "of", "on", "in", "to", "for", "with", "at",
        "by", "from", "that", "which", "as", "into", "over", "after", "before", "about",
        "its", "their", "his", "her", "our", "this", "these", "those", "is", "was", "were"
    };

    private static readonly HashSet<string> Conjunctions =
        new(StringComparer.OrdinalIgnoreCase) { "and", "or", "but", "with", "for", "while", "plus" };

    private static string TrimDanglingWords(string title)
    {
        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

        while (words.Count > 2 && DanglingWords.Contains(Bare(words[^1])))
            words.RemoveAt(words.Count - 1);

        // A truncated conjunction phrase dangles even when its last word doesn't:
        // "…flow issues and agreed" ends on a verb and still reads as cut off. Drop
        // from the conjunction, provided enough title survives to mean something.
        var tail = Math.Max(0, words.Count - 3);
        for (var i = words.Count - 1; i >= tail; i--)
        {
            if (!Conjunctions.Contains(Bare(words[i])) || i < 3) continue;

            words.RemoveRange(i, words.Count - i);
            break;
        }

        return string.Join(' ', words);
    }

    private static string Bare(string word) => word.Trim(',', ';', ':', '.');
}
