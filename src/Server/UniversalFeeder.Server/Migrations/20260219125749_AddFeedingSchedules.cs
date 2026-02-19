using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversalFeeder.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedingSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FeederId = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeOfDay = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    AmountInGrams = table.Column<double>(type: "REAL", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedules_Feeders_FeederId",
                        column: x => x.FeederId,
                        principalTable: "Feeders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_FeederId",
                table: "Schedules",
                column: "FeederId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Schedules");
        }
    }
}
