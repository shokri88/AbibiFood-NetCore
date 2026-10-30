namespace AbibiFood.Domain.Entities;

public sealed class GroupFood
{
    public int Id { get; set; }
    public int BranchGroupId { get; set; }
    public int BranchFoodId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
