using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportLab.Migrations
{
    /// <inheritdoc />
    public partial class AddCapacityToSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Booked",
                table: "WorkoutSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "WorkoutSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Booked",
                table: "WorkoutSlots");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "WorkoutSlots");
        }
    }
}
