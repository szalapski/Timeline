using System.Diagnostics;

namespace Sz.Timeline.Client.ViewModels;

public class TimelineVM
{
    public TimelineVM(string? itemsText)
    {
        Items = itemsText == null ? new() : ParseTimelineEvents(itemsText).OrderBy(i => i.Date).ToList();
        if (!Items.Any()) throw new InvalidOperationException("At least one event with a date is required.");
    }

    public List<TimelineEvent> Items { get; set; } = new();

    public DateOnly Start => new(RoundToIntervalYearOnOrBefore(Items.First().Date.Year), 1, 1);
    public DateOnly End => new(RoundToIntervalYearOnOrAfter(Items.Last().Date.Year + 1), 1, 1);

    public int StartYear => Start.Year;
    public int EndYear => End.Year;
    public int YearsOfEventsCount => Items.Last().Date.Year - Items.First().Date.Year;

    public int YearsInterval
    {
        get
        {
            if (YearsOfEventsCount < 20) return 1;
            if (YearsOfEventsCount < 40) return 2;
            if (YearsOfEventsCount < 100) return 5;
            if (YearsOfEventsCount < 200) return 10;
            if (YearsOfEventsCount < 400) return 20;
            if (YearsOfEventsCount < 1000) return 50;
            if (YearsOfEventsCount < 2000) return 100;
            if (YearsOfEventsCount < 4000) return 200;
            if (YearsOfEventsCount < 10000) return 500;
            if (YearsOfEventsCount < 20000) return 1000;
            if (YearsOfEventsCount < 40000) return 2000;
            return 5000;
        }
    }

    public int DaysInTimeline => End.DayNumber - Start.DayNumber;

    public decimal GetPositionPercentage(DateOnly date)
    {
        if (date < Start || date > End) throw new ArgumentOutOfRangeException(nameof(date));
        return 100m * ((decimal)(date.DayNumber - Start.DayNumber)) / DaysInTimeline;
    }

    public int RoundToIntervalYearOnOrBefore(int year) => year / YearsInterval * YearsInterval;
    public int RoundToIntervalYearOnOrAfter(int year) => (year+YearsInterval-1) / YearsInterval * YearsInterval;


    private const decimal tooCloseTolerancePercent = 1.5m;

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
            foreach(decimal percentage in LabelPositionPercents.OrderBy(d=>d))
            {
                decimal result = percentage;
                decimal lastPosition = results.LastOrDefault();
                result = NudgeDownToAvoidOverlap(results, result, lastPosition);
                results.Add(result);
            }
            return results;
        }
    }

    private static decimal NudgeDownToAvoidOverlap(List<decimal> results, decimal result, decimal lastPosition)
    {
        if (results.Any() && result < lastPosition + tooCloseTolerancePercent)
            result = lastPosition + tooCloseTolerancePercent;
        return result;
    }

    public record TimelineEvent(DateOnly Date, string? Name, string Description)
    {
    }

}