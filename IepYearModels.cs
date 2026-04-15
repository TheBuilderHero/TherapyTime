using System.Text.Json;

namespace TherapyTime;

public class IepMonthDefinition
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public override string ToString() => $"{Name} ({StartDate:MM/dd/yyyy} - {EndDate:MM/dd/yyyy})";
}

public class IepYearFile
{
    public string SchemaVersion { get; set; } = "2.0";
    public int SchoolYear { get; set; }
    public DateTime SchoolYearStartDate { get; set; }
    public DateTime SchoolYearEndDate { get; set; }
    public List<IepMonthDefinition> Months { get; set; } = new List<IepMonthDefinition>();
    public List<StudentIepData> Students { get; set; } = new List<StudentIepData>();
}

public static class IepYearFileManager
{
    private static readonly JsonSerializerOptions JsonReadOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    public static string GetFileName(int schoolYear)
    {
        return $"IEP_Year_{schoolYear}.json";
    }

    public static int InferSchoolYear(DateTime date)
    {
        return date.Month >= 8 ? date.Year : date.Year - 1;
    }

    public static List<IepMonthDefinition> BuildDefaultMonths(int schoolYear)
    {
        var months = new List<IepMonthDefinition>();
        DateTime firstMonth = new DateTime(schoolYear, 8, 1);

        for (int i = 0; i < 12; i++)
        {
            DateTime start = firstMonth.AddMonths(i);
            DateTime end = start.AddMonths(1).AddDays(-1);
            months.Add(new IepMonthDefinition
            {
                Name = start.ToString("MMMM"),
                StartDate = start,
                EndDate = end
            });
        }

        return months;
    }

    public static IepYearFile CreateYear(int schoolYear, List<IepMonthDefinition> months)
    {
        var orderedMonths = months
            .OrderBy(m => m.StartDate)
            .Select(m => new IepMonthDefinition
            {
                Name = m.Name,
                StartDate = m.StartDate.Date,
                EndDate = m.EndDate.Date
            })
            .ToList();

        DateTime schoolStart = orderedMonths.Min(m => m.StartDate);
        DateTime schoolEnd = orderedMonths.Max(m => m.EndDate);

        return new IepYearFile
        {
            SchoolYear = schoolYear,
            SchoolYearStartDate = schoolStart,
            SchoolYearEndDate = schoolEnd,
            Months = orderedMonths,
            Students = new List<StudentIepData>()
        };
    }

    public static string SaveToJson(IepYearFile yearFile)
    {
        return JsonSerializer.Serialize(yearFile, JsonWriteOptions);
    }

    public static bool TryLoadFromJson(string json, out IepYearFile? yearFile)
    {
        yearFile = null;

        try
        {
            var parsed = JsonSerializer.Deserialize<IepYearFile>(json, JsonReadOptions);
            if (parsed == null)
            {
                return false;
            }

            if (parsed.Months == null || parsed.Months.Count == 0)
            {
                return false;
            }

            parsed.Months = parsed.Months
                .OrderBy(m => m.StartDate)
                .ToList();

            parsed.Students ??= new List<StudentIepData>();

            if (parsed.SchoolYear == 0)
            {
                parsed.SchoolYear = InferSchoolYear(parsed.Months.Min(m => m.StartDate));
            }

            if (parsed.SchoolYearStartDate == default)
            {
                parsed.SchoolYearStartDate = parsed.Months.Min(m => m.StartDate).Date;
            }

            if (parsed.SchoolYearEndDate == default)
            {
                parsed.SchoolYearEndDate = parsed.Months.Max(m => m.EndDate).Date;
            }

            yearFile = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool ValidateMonths(List<IepMonthDefinition> months, out string error)
    {
        error = string.Empty;

        if (months.Count != 12)
        {
            error = "Exactly 12 IEP months are required.";
            return false;
        }

        var ordered = months.OrderBy(m => m.StartDate).ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].StartDate.Date > ordered[i].EndDate.Date)
            {
                error = $"{ordered[i].Name}: start date must be before or equal to end date.";
                return false;
            }

            if (i > 0 && ordered[i].StartDate.Date <= ordered[i - 1].EndDate.Date)
            {
                error = $"{ordered[i].Name} overlaps with {ordered[i - 1].Name}.";
                return false;
            }
        }

        return true;
    }
}
