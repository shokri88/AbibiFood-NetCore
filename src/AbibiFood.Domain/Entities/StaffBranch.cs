namespace AbibiFood.Domain.Entities;

public sealed class StaffBranch
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public int BranchId { get; set; }
    public bool IsActive { get; set; } = true;
}
