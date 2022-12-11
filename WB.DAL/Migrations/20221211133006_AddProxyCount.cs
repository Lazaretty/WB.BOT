using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WB.DAL.Migrations
{
    public partial class AddProxyCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SuccessfulUses",
                table: "Proxies",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuccessfulUses",
                table: "Proxies");
        }
    }
}
