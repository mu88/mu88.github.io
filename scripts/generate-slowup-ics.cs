using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

const string nationalUrl = "https://www.slowup.ch/national/de.html";
const string defaultOutputPath = "_site/slowUp.ics";

var options = SlowUpOptions.Parse(args, nationalUrl, defaultOutputPath);
using var cancellationSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

using var httpClient = CreateHttpClient();
var generator = new SlowUpFeedGenerator(httpClient, TimeProvider.System);
var calendar = await generator.GenerateAsync(options.NationalUrl, cancellationSource.Token);

Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath) ?? ".");
await File.WriteAllTextAsync(options.OutputPath, calendar, new UTF8Encoding(false), cancellationSource.Token);
Console.WriteLine($"Wrote {options.OutputPath}");

static HttpClient CreateHttpClient()
{
    var handler = new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
    };

    var httpClient = new HttpClient(handler)
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("slowup-feed", "1.0"));
    return httpClient;
}

sealed record SlowUpOptions(Uri NationalUrl, string OutputPath)
{
    public static SlowUpOptions Parse(string[] args, string nationalUrl, string defaultOutputPath)
    {
        var outputPath = defaultOutputPath;
        for (var index = 0; index < args.Length; index++)
        {
            var current = args[index];
            if (current is not ("--output" or "-o"))
            {
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException("Missing value for --output.");
            }

            outputPath = args[index + 1];
            index++;
        }

        return new SlowUpOptions(new Uri(nationalUrl), outputPath);
    }
}

sealed class SlowUpFeedGenerator(HttpClient httpClient, TimeProvider timeProvider)
{
    private const string ZurichTimeZoneId = "Europe/Zurich";

    private static readonly Regex AnchorRegex = new("<a\\b[^>]*href=\"(?<href>[^\"]+)\"[^>]*>(?<content>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private static readonly Regex TitleRegex = new("^slowUp\\s+(?<title>.+?)\\s+(?<date>\\d{2}\\.\\d{2}\\.\\d{4})$", RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private static readonly Regex TimeRegex = new("(?<start>\\d{1,2}:\\d{2})\\s*[-–—]\\s*(?<end>\\d{1,2}:\\d{2})", RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private static readonly Regex H1Regex = new("<h1\\b[^>]*>(?<content>.*?)</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private static readonly Regex TimeHeadingRegex = new("<h2\\b[^>]*class=\"[^\"]*mt-3[^\"]*\"[^>]*>(?<content>.*?)</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Uri BaseUri = new("https://www.slowup.ch/");

    public async Task<string> GenerateAsync(Uri nationalUrl, CancellationToken cancellationToken)
    {
        var nationalHtml = await GetStringAsync(nationalUrl, cancellationToken);
        var eventLinks = ParseEventLinks(nationalHtml);
        if (eventLinks.Count == 0)
        {
            throw new InvalidOperationException("No slowUp event links were found on the national page.");
        }

        var events = new List<SlowUpEvent>(eventLinks.Count);
        foreach (var eventLink in eventLinks)
        {
            var eventHtml = await GetStringAsync(eventLink, cancellationToken);
            events.Add(ParseEvent(eventHtml, eventLink));
        }

        return BuildCalendar(events);
    }

    private async Task<string> GetStringAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static IReadOnlyList<Uri> ParseEventLinks(string html)
    {
        var links = new List<Uri>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in AnchorRegex.Matches(html))
        {
            var href = WebUtility.HtmlDecode(match.Groups["href"].Value).Trim();
            if (!IsEventLink(href) || !seen.Add(href))
            {
                continue;
            }

            var content = match.Groups["content"].Value;
            if (!content.Contains("card__title", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            links.Add(new Uri(BaseUri, href));
        }

        return links;
    }

    private static bool IsEventLink(string href)
    {
        return Regex.IsMatch(href, "^/[a-z0-9-]+/(de|fr|it)\\.html$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) &&
               !href.StartsWith("/national/", StringComparison.OrdinalIgnoreCase);
    }

    private static SlowUpEvent ParseEvent(string html, Uri eventUri)
    {
        var title = ParseTitle(html, eventUri);
        var eventDate = ParseDate(html, eventUri);
        var (startTime, endTime) = ParseTimeWindow(html, eventUri);
        var description = BuildDescription(eventUri);

        return new SlowUpEvent(title, eventDate, startTime, endTime, eventUri, description);
    }

    private static string ParseTitle(string html, Uri eventUri)
    {
        var h1 = ExtractTagContent(html, H1Regex, eventUri, "h1");
        var normalized = NormalizeText(StripTags(h1));
        var match = TitleRegex.Match(normalized);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not parse the slowUp title from {eventUri}.");
        }

        return $"slowUp {match.Groups["title"].Value.Trim()}";
    }

    private static DateOnly ParseDate(string html, Uri eventUri)
    {
        var h1 = ExtractTagContent(html, H1Regex, eventUri, "h1");
        var normalized = NormalizeText(StripTags(h1));
        var match = TitleRegex.Match(normalized);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not parse the slowUp date from {eventUri}.");
        }

        return DateOnly.ParseExact(match.Groups["date"].Value, "dd.MM.yyyy", CultureInfo.InvariantCulture);
    }

    private static (TimeOnly Start, TimeOnly End) ParseTimeWindow(string html, Uri eventUri)
    {
        var heading = ExtractTagContent(html, TimeHeadingRegex, eventUri, "time heading");
        var normalized = NormalizeText(StripTags(heading));
        var match = TimeRegex.Match(normalized);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not parse the slowUp time window from {eventUri}.");
        }

        return (
            TimeOnly.ParseExact(match.Groups["start"].Value, "H:mm", CultureInfo.InvariantCulture),
            TimeOnly.ParseExact(match.Groups["end"].Value, "H:mm", CultureInfo.InvariantCulture));
    }

    private static string ExtractTagContent(string html, Regex regex, Uri eventUri, string tagName)
    {
        var match = regex.Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find {tagName} on {eventUri}.");
        }

        return match.Groups["content"].Value;
    }

    private static string StripTags(string value)
    {
        return HtmlTagRegex.Replace(value, " ");
    }

    private static string NormalizeText(string value)
    {
        return WhitespaceRegex.Replace(WebUtility.HtmlDecode(value), " ").Trim();
    }

    private static string BuildDescription(Uri eventUri)
    {
        return $"Detail page: {eventUri}{Environment.NewLine}Source: slowUp.ch";
    }

    private string BuildCalendar(IReadOnlyList<SlowUpEvent> events)
    {
        var builder = new StringBuilder();
        AppendLine(builder, "BEGIN:VCALENDAR");
        AppendLine(builder, "PRODID:-//mu88//slowUp Calendar Feed//EN");
        AppendLine(builder, "VERSION:2.0");
        AppendLine(builder, "CALSCALE:GREGORIAN");
        AppendLine(builder, "METHOD:PUBLISH");
        AppendLine(builder, "X-WR-CALNAME:slowUp Events");
        AppendLine(builder, "X-WR-TIMEZONE:Europe/Zurich");
        AppendZurichTimeZone(builder);

        foreach (var slowUpEvent in events)
        {
            AppendEvent(builder, slowUpEvent);
        }

        AppendLine(builder, "END:VCALENDAR");
        return builder.ToString();
    }

    private void AppendEvent(StringBuilder builder, SlowUpEvent slowUpEvent)
    {
        var localStart = slowUpEvent.Date.ToDateTime(slowUpEvent.StartTime);
        var localEnd = slowUpEvent.Date.ToDateTime(slowUpEvent.EndTime);
        var utcStamp = timeProvider.GetUtcNow().UtcDateTime;

        AppendLine(builder, "BEGIN:VEVENT");
        AppendLine(builder, $"UID:{BuildUid(slowUpEvent)}");
        AppendLine(builder, $"DTSTAMP:{utcStamp:yyyyMMdd'T'HHmmss'Z'}");
        AppendLine(builder, $"DTSTART;TZID={ZurichTimeZoneId}:{localStart:yyyyMMdd'T'HHmmss}");
        AppendLine(builder, $"DTEND;TZID={ZurichTimeZoneId}:{localEnd:yyyyMMdd'T'HHmmss}");
        AppendLine(builder, $"SUMMARY:{EscapeIcsText(slowUpEvent.Title)}");
        AppendLine(builder, $"DESCRIPTION:{EscapeIcsText(slowUpEvent.Description)}");
        AppendLine(builder, $"URL:{slowUpEvent.Url}");
        AppendLine(builder, "STATUS:CONFIRMED");
        AppendLine(builder, "TRANSP:TRANSPARENT");
        AppendLine(builder, "END:VEVENT");
    }

    private static void AppendZurichTimeZone(StringBuilder builder)
    {
        AppendLine(builder, "BEGIN:VTIMEZONE");
        AppendLine(builder, $"TZID:{ZurichTimeZoneId}");
        AppendLine(builder, "X-LIC-LOCATION:Europe/Zurich");
        AppendLine(builder, "BEGIN:DAYLIGHT");
        AppendLine(builder, "TZOFFSETFROM:+0100");
        AppendLine(builder, "TZOFFSETTO:+0200");
        AppendLine(builder, "TZNAME:CEST");
        AppendLine(builder, "DTSTART:19700329T020000");
        AppendLine(builder, "RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=-1SU");
        AppendLine(builder, "END:DAYLIGHT");
        AppendLine(builder, "BEGIN:STANDARD");
        AppendLine(builder, "TZOFFSETFROM:+0200");
        AppendLine(builder, "TZOFFSETTO:+0100");
        AppendLine(builder, "TZNAME:CET");
        AppendLine(builder, "DTSTART:19701025T030000");
        AppendLine(builder, "RRULE:FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU");
        AppendLine(builder, "END:STANDARD");
        AppendLine(builder, "END:VTIMEZONE");
    }

    private static string BuildUid(SlowUpEvent slowUpEvent)
    {
        var slug = slowUpEvent.Url.AbsolutePath.Trim('/').Replace('/', '-');
        return $"{slug}-{slowUpEvent.Date:yyyyMMdd}@mu88.github.io";
    }

    private static string EscapeIcsText(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private static void AppendLine(StringBuilder builder, string line)
    {
        const int maxOctetsPerLine = 75;

        var currentLine = new StringBuilder();
        var currentOctets = 0;
        var remainingBudget = maxOctetsPerLine;
        var continuation = false;

        foreach (var rune in line.EnumerateRunes())
        {
            var runeOctets = GetUtf8OctetCount(rune);
            if (currentOctets > 0 && currentOctets + runeOctets > remainingBudget)
            {
                AppendPhysicalLine(builder, currentLine);
                currentLine.Clear();
                currentLine.Append(' ');
                currentOctets = 1;
                remainingBudget = maxOctetsPerLine;
                continuation = true;
            }

            currentLine.Append(rune.ToString());
            currentOctets += runeOctets;
        }

        if (continuation && currentLine.Length == 1)
        {
            AppendPhysicalLine(builder, currentLine);
            return;
        }

        AppendPhysicalLine(builder, currentLine);
    }

    private static void AppendPhysicalLine(StringBuilder builder, StringBuilder line)
    {
        builder.Append(line);
        builder.Append("\r\n");
    }

    private static int GetUtf8OctetCount(Rune rune)
    {
        Span<byte> buffer = stackalloc byte[4];
        if (!rune.TryEncodeToUtf8(buffer, out var bytesWritten))
        {
            throw new InvalidOperationException("Could not encode rune to UTF-8.");
        }

        return bytesWritten;
    }

}

sealed record SlowUpEvent(string Title, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, Uri Url, string Description);
