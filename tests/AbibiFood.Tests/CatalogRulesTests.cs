using AbibiFood.Domain.Rules;
using AbibiFood.Domain.Entities;
namespace AbibiFood.Tests;
public sealed class CatalogRulesTests
{
    [Theory]
    [InlineData(-1, 0)] [InlineData(100, 100)] [InlineData(100, 101)] [InlineData(100, -1)]
    public void InvalidDiscountIsRejected(decimal price, decimal discount) => Assert.Throws<ArgumentException>(() => CatalogRules.Price(price, discount));
    [Fact] public void ValidDiscountAndNoDiscountAreAccepted() { CatalogRules.Price(100, 80); CatalogRules.Price(0, null); }
    [Fact] public void CapacityCannotFallBelowCommitments() => Assert.Throws<ArgumentException>(() => CatalogRules.Capacity(4, 2, 3, 1));
    [Fact] public void AddonCannotBeItself() => Assert.Throws<ArgumentException>(() => CatalogRules.Addon(1, 1, 2, 2, 1));
    [Fact] public void AddonCannotCrossBranches() => Assert.Throws<ArgumentException>(() => CatalogRules.Addon(1, 2, 1, 2, 1));
    [Fact] public void SharedItemCapacityIsOneDailyRecord() { var capacity = new DailyCapacity {Total=10,Reserved=2,Sold=3};Assert.Equal(5,capacity.Remaining); }
}
