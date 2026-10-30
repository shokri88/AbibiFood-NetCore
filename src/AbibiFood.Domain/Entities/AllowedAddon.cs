namespace AbibiFood.Domain.Entities;

public sealed class AllowedAddon
{
    public int Id { get; set; }
    public int ParentBranchFoodId { get; set; }
    public int AddonBranchFoodId { get; set; }
    public int MaxQuantity { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}
