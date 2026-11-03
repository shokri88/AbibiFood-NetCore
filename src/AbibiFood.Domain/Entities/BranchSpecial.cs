namespace AbibiFood.Domain.Entities;
public sealed class BranchSpecial
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int BranchFoodId { get; set; }
    public BranchFood BranchFood { get; set; } = null!;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
