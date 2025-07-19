using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Eventify.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSubscriptionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanCreateEvents",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SubscriptionPlans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "HasAdvancedAnalytics",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPrioritySupport",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxEventsPerMonth",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxParticipantsPerEvent",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "CanCreateEvents", "Description", "DurationInDays", "HasAdvancedAnalytics", "HasPrioritySupport", "IsActive", "MaxDailyJoins", "MaxEventsPerMonth", "MaxParticipantsPerEvent", "Name", "Price", "SortOrder" },
                values: new object[,]
                {
                    { 1, true, "Küçük etkinlikler için ideal başlangıç paketi", 30, false, false, true, 3, 5, 50, "Basic", 29.99m, 1 },
                    { 2, true, "Orta ölçekli etkinlikler için profesyonel paket", 30, false, true, true, 10, 20, 200, "Pro", 79.99m, 2 },
                    { 3, true, "Büyük etkinlikler için premium organizatör paketi", 30, true, true, true, 50, 100, 1000, "Organizer+", 199.99m, 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "CanCreateEvents",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "HasAdvancedAnalytics",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "HasPrioritySupport",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxEventsPerMonth",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxParticipantsPerEvent",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "SubscriptionPlans");
        }
    }
}
