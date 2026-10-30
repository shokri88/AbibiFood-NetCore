namespace AbibiFood.Domain.Entities;

public sealed class BranchGroup
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Title { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
