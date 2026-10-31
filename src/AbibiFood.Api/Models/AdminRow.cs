namespace AbibiFood.Api.Models;

public sealed record AdminRow(string Title, string Details, string EditUrl,
    string? DeleteKind = null, int? DeleteId = null, string? ImageUrl = null);
public sealed record DeleteButton(string Kind, string Id, string ReturnUrl);
