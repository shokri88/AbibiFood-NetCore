namespace AbibiFood.Api.Models;

public sealed record AdminRow(string Title, string Details, string EditUrl,
    string? DeleteKind = null, int? DeleteId = null, string? ImageUrl = null, string? ThumbnailUrl = null, bool? IsActive = null, int? SortOrder = null,
    decimal? Price = null, decimal? DiscountPrice = null);
public sealed record DeleteButton(string Kind, string Id, string ReturnUrl);

public sealed record AdminTableOptions(bool ShowDetails = true, string DetailsTitle = "جزئیات",
    bool ShowPrices = false, bool ShowActive = false, bool ShowOrder = false);
