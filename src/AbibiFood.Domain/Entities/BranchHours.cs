namespace AbibiFood.Domain.Entities;

public sealed class BranchHours
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public HoursKind Kind { get; set; }
    public DayOfWeek Day { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public bool IsActive { get; set; } = true;
}
