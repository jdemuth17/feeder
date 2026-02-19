using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversalFeeder.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddFeederUniqueId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UniqueId",
                table: "Feeders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UniqueId",
                table: "Feeders");
        }
    }
}
