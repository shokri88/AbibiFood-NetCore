using System.ComponentModel.DataAnnotations;
using AbibiFood.Domain.Entities;
namespace AbibiFood.Application.Models;

public sealed class BranchInput
{
    public int Id { get; set; }
    [Required(ErrorMessage = "این فیلد الزامی است."), StringLength(150)] public string Name { get; set; } = "";
    [Required(ErrorMessage = "این فیلد الزامی است."), StringLength(500)] public string Address { get; set; } = "";
    [StringLength(2000)] public string Description { get; set; } = "";
    [Range(-90, 90)] public decimal? Latitude { get; set; }
    [Range(-180, 180)] public decimal? Longitude { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class FoodInput
{
    public int Id { get; set; }
    [Required(ErrorMessage = "این فیلد الزامی است."), StringLength(150)] public string Name { get; set; } = "";
    [StringLength(200)] public string Title { get; set; } = "";
    [StringLength(4000)] public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
public sealed class ImageInput
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "انتخاب این مورد الزامی است.")] public int FoodId { get; set; }
    [Required(ErrorMessage = "این فیلد الزامی است."), StringLength(500)] public string Path { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class GroupInput
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "انتخاب این مورد الزامی است.")] public int BranchId { get; set; }
    [Required(ErrorMessage = "این فیلد الزامی است."), StringLength(150)] public string Title { get; set; } = "";
    [StringLength(500)] public string ImagePath { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class BranchFoodInput
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "انتخاب این مورد الزامی است.")] public int BranchId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "انتخاب این مورد الزامی است.")] public int FoodId { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public bool CanSellStandalone { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? Version { get; set; }
}
public sealed class GroupFoodInput
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "انتخاب این مورد الزامی است.")] public int BranchGroupId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "انتخاب این مورد الزامی است.")] public int BranchFoodId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class AddonInput
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "انتخاب این مورد الزامی است.")] public int ParentBranchFoodId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "انتخاب این مورد الزامی است.")] public int AddonBranchFoodId { get; set; }
    public int MaxQuantity { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}
public sealed class CapacityInput
{
    [Range(1, int.MaxValue, ErrorMessage = "انتخاب این مورد الزامی است.")] public int BranchFoodId { get; set; }
    public DateOnly Date { get; set; }
    public int Total { get; set; }
    public int WarningThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public string? Version { get; set; }
}
public sealed class HoursInput
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "انتخاب این مورد الزامی است.")] public int BranchId { get; set; }
    public HoursKind Kind { get; set; }
    public DayOfWeek Day { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public bool IsActive { get; set; } = true;
}
