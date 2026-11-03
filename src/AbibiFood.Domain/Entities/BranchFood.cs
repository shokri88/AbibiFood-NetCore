namespace AbibiFood.Domain.Entities;

public sealed class BranchFood
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int FoodId { get; set; }
    public Food Food { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public bool CanSellStandalone { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int? DefaultDailyCapacity { get; set; }
    public int DefaultCapacityWarningThreshold { get; set; } = 5;
    public bool IsDailyCapacityActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}
