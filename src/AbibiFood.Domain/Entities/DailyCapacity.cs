namespace AbibiFood.Domain.Entities;

public sealed class DailyCapacity
{
    public int Id { get; set; }
    public int BranchFoodId { get; set; }
    public DateOnly Date { get; set; }
    public int Total { get; set; }
    public int Reserved { get; set; }
    public int Sold { get; set; }
    public int WarningThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public int Remaining => Math.Max(0, Total - Reserved - Sold);
}
