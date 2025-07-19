using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventify.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PendingChang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hobby",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hobby", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserHobbies",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    HobbyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHobbies", x => new { x.UserId, x.HobbyId });
                    table.ForeignKey(
                        name: "FK_UserHobbies_Hobby_HobbyId",
                        column: x => x.HobbyId,
                        principalTable: "Hobby",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserHobbies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserHobbies_HobbyId",
                table: "UserHobbies",
                column: "HobbyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserHobbies");

            migrationBuilder.DropTable(
                name: "Hobby");
        }
    }
}
