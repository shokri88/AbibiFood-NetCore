using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbibiFood.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecurringDailyCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultCapacityWarningThreshold",
                table: "BranchFoods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultDailyCapacity",
                table: "BranchFoods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDailyCapacityActive",
                table: "BranchFoods",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultCapacityWarningThreshold",
                table: "BranchFoods");

            migrationBuilder.DropColumn(
                name: "DefaultDailyCapacity",
                table: "BranchFoods");

            migrationBuilder.DropColumn(
                name: "IsDailyCapacityActive",
                table: "BranchFoods");
        }
    }
}
