namespace AbibiFood.Application.Abstractions;

public interface IUserScope
{
    string UserId { get; }
    bool IsManager { get; }
    Task EnsureBranchAsync(int branchId, CancellationToken ct);
}
