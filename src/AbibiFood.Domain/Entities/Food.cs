namespace AbibiFood.Domain.Entities;

public sealed class Food
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public List<FoodImage> Images { get; set; } = [];
}
