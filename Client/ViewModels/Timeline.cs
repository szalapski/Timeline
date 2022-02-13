using System.Diagnostics;

namespace Sz.Timeline.Client.ViewModels;

public class TimelineVM
{
    public TimelineVM(string? itemsText)
    {
        Console.WriteLine(itemsText);

        Items = itemsText == null ? new() : ParseTimelineEvents(itemsText).OrderBy(i => i.Date).ToList();
        if (!Items.Any()) throw new InvalidOperationException("At least one event with a date is required.");
    }

    public List<TimelineEvent> Items { get; set; } = new();

    public DateOnly? Start => Items.Any() ? new(Items.First().Date.Year, 1, 1) : null;
    public DateOnly? End => Items.Any() ? new(Items.Last().Date.Year + 1, 1, 1) : null;

    public int? StartYear => Start?.Year;
    public int? DisplayStartYear => StartYear == null ? null : StartYear - 1;
    public int? EndYear => End?.Year;

    public int? DaysInTimeline => Items.Any() ? End!.Value.DayNumber - Start!.Value.DayNumber : null;

    public decimal GetPositionPercentage(DateOnly date)
    {
        if (date < Start || date > End) throw new ArgumentOutOfRangeException(nameof(date));
        return Items.Any() ? 100m * ((decimal)(date.DayNumber - Start!.Value.DayNumber)) / DaysInTimeline!.Value : 0;
    }



    private const decimal tooCloseTolerancePercent = 2m;

    private static IEnumerable<TimelineEvent> ParseTimelineEvents(string itemsText)
    {
        Stopwatch watch = Stopwatch.StartNew();
        foreach (string eventText in itemsText.Trim().Split(Environment.NewLine))
        {
            string[] tokens = eventText.Trim().Split(null);

            bool gotDate = DateOnly.TryParse(tokens.FirstOrDefault(), out DateOnly date);
            if (!gotDate)
            {
                // TODO: year-only events
                bool gotYear = int.TryParse(tokens.FirstOrDefault(), out int year);
                if (gotYear) date = new DateOnly(year, 1, 1);
                else continue;
            }

            string? name = tokens.Skip(1).FirstOrDefault();

            string theRest = string.Join(" ", tokens.Skip(2));

            yield return new TimelineEvent(date, name, theRest);
        }
        watch.Stop();
        Console.WriteLine($"parsing took {watch.ElapsedMilliseconds} ms");
    }

    public List<decimal> LabelPositionPercents => Items.Select(i => GetPositionPercentage(i.Date)).ToList();
    public List<decimal> AdjustedLabelPositionPercents { get
        {
            List<decimal> results = new();
            foreach(decimal percentage in LabelPositionPercents)
            {
                decimal result = percentage;
                Console.WriteLine($"{percentage}");
                if (results.Any(p => percentage < tooCloseTolerancePercent + p))
                    result = percentage + tooCloseTolerancePercent;
                results.Add(result);
            }
            return results;
        }
    }

    public record TimelineEvent(DateOnly Date, string? Name, string Description)
    {
    }

}