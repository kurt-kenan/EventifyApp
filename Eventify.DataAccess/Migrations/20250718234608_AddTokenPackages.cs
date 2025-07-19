using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Eventify.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "TokenTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "TokenTransactions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "TokenTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TokenPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenAmount = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPopular = table.Column<bool>(type: "bit", nullable: false),
                    BonusText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenPackages", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TokenPackages",
                columns: new[] { "Id", "BonusText", "Description", "IsActive", "IsPopular", "Name", "Price", "SortOrder", "TokenAmount" },
                values: new object[,]
                {
                    { 1, null, "Küçük paket - Hızlı başlangıç", true, false, "500 Jeton", 19.99m, 1, 500 },
                    { 2, "En Popüler", "Orta paket - En popüler seçim", true, true, "1000 Jeton", 34.99m, 2, 1000 },
                    { 3, "%10 Bonus", "Büyük paket - %10 bonus", true, false, "5000 Jeton", 149.99m, 3, 5000 },
                    { 4, "%15 Bonus", "Mega paket - %15 bonus", true, false, "10000 Jeton", 279.99m, 4, 10000 },
                    { 5, "%20 Bonus", "Ultra paket - %20 bonus", true, false, "50000 Jeton", 1199.99m, 5, 50000 },
                    { 6, "%25 Bonus", "Maksimum paket - %25 bonus", true, false, "100000 Jeton", 1999.99m, 6, 100000 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenPackages");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "TokenTransactions");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "TokenTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "TokenTransactions");
        }
    }
}
