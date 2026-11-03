namespace AbibiFood.Domain.Entities;
public sealed class BranchBanner
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int BranchFoodId { get; set; }
    public BranchFood BranchFood { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
