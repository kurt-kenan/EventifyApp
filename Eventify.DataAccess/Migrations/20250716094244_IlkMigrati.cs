using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventify.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class IlkMigrati : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventHobbies",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false),
                    HobbyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventHobbies", x => new { x.EventId, x.HobbyId });
                    table.ForeignKey(
                        name: "FK_EventHobbies_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventHobbies_Hobby_HobbyId",
                        column: x => x.HobbyId,
                        principalTable: "Hobby",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventHobbies_HobbyId",
                table: "EventHobbies",
                column: "HobbyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventHobbies");
        }
    }
}
