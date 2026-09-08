using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryAndBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "PurchaseRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DepartmentBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Department = table.Column<string>(type: "text", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SpentAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentBudgets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentBudgets_Department",
                table: "DepartmentBudgets",
                column: "Department",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentBudgets");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "PurchaseRequests");
        }
    }
}
