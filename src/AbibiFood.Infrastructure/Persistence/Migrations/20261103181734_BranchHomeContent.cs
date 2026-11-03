using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbibiFood.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BranchHomeContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BranchBanners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    BranchFoodId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchBanners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchBanners_BranchFoods_BranchFoodId",
                        column: x => x.BranchFoodId,
                        principalTable: "BranchFoods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchBanners_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BranchSpecials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    BranchFoodId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchSpecials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchSpecials_BranchFoods_BranchFoodId",
                        column: x => x.BranchFoodId,
                        principalTable: "BranchFoods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchSpecials_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchBanners_BranchFoodId",
                table: "BranchBanners",
                column: "BranchFoodId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchBanners_BranchId",
                table: "BranchBanners",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchSpecials_BranchFoodId",
                table: "BranchSpecials",
                column: "BranchFoodId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchSpecials_BranchId_BranchFoodId",
                table: "BranchSpecials",
                columns: new[] { "BranchId", "BranchFoodId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchBanners");

            migrationBuilder.DropTable(
                name: "BranchSpecials");
        }
    }
}
