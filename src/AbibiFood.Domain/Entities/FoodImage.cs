namespace AbibiFood.Domain.Entities;

public sealed class FoodImage
{
    public int Id { get; set; }
    public int FoodId { get; set; }
    public string Path { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
