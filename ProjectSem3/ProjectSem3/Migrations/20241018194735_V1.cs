using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSem3.Migrations
{
    public partial class V1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrderPrice",
                table: "Order",
                newName: "Price");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Order",
                type: "int",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Order",
                newName: "OrderPrice");
            migrationBuilder.AlterColumn<bool>(
                name: "Status",
                table: "Order",
                type: "bit",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
