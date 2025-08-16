namespace BairroBrasilAnalytics.Dtos;

public class CategoryCreateDto
{
    public string Name { get; set; } = string.Empty;
}

public class RecordCreateDto
{
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class RecordUpdateDto
{
    public DateTime? Timestamp { get; set; }
    public string? Source { get; set; }
    public string? CategoryName { get; set; }
    public decimal? Amount { get; set; }
    public string? Notes { get; set; }
}