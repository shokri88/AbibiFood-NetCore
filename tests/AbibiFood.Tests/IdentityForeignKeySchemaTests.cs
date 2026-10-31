using AbibiFood.Domain.Entities;
using AbibiFood.Infrastructure.Persistence;
using AbibiFood.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace AbibiFood.Tests;

public sealed class IdentityForeignKeySchemaTests
{
    [Fact]
    public void SqlModel_ForeignKeyColumnTypesMatchReferencedKeys()
    {
        // مدل provider واقعی SQL بررسی می‌شود؛ این تست اتصال یا اجرای SQL ندارد.
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=SchemaOnly;Integrated Security=True").Options);
        var staff = db.Model.FindEntityType(typeof(StaffBranch))!;
        Assert.Equal("nvarchar(450)", staff.FindProperty(nameof(StaffBranch.UserId))!.GetColumnType());
        foreach (var entity in db.Model.GetEntityTypes())
        foreach (var foreignKey in entity.GetForeignKeys())
        for (var index = 0; index < foreignKey.Properties.Count; index++)
            Assert.Equal(foreignKey.PrincipalKey.Properties[index].GetColumnType(),
                foreignKey.Properties[index].GetColumnType());
    }

    [Fact]
    public void InitialMigration_ForeignKeyColumnTypesMatchReferencedKeys()
    {
        // InMemory طول FK را enforce نمی‌کند؛ خود عملیات Migration هم باید مستقل بررسی شود.
        var tables = new InitialCatalogAndIdentity().UpOperations.OfType<CreateTableOperation>()
            .ToDictionary(table => table.Name);
        var staff = tables["StaffBranches"];
        Assert.Equal("nvarchar(450)", staff.Columns.Single(column => column.Name == "UserId").ColumnType);
        foreach (var table in tables.Values)
        foreach (var foreignKey in table.ForeignKeys)
        for (var index = 0; index < foreignKey.Columns.Length; index++)
        {
            var referenced = tables[foreignKey.PrincipalTable].Columns
                .Single(column => column.Name == foreignKey.PrincipalColumns![index]);
            var dependent = table.Columns.Single(column => column.Name == foreignKey.Columns[index]);
            Assert.Equal(referenced.ColumnType, dependent.ColumnType);
        }
    }
}
