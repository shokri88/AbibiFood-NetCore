namespace AbibiFood.Domain.Rules;

public sealed class ConflictException(string message) : Exception(message);
