namespace AbibiFood.Domain.Entities;

public sealed class ErrorLog
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string CorrelationId { get; set; } = "";
    public string ActorId { get; set; } = "";
    public string RequestPath { get; set; } = "";
    public string ExceptionType { get; set; } = "";
    public string Message { get; set; } = "";
    public string StackTrace { get; set; } = "";
}
