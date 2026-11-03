using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportLab.Migrations
{
    
    public partial class AddRatingAndCategoryToProduct : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Tariffs_TariffId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSlots_Users_ClientId",
                table: "WorkoutSlots");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "WorkoutSlots",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkoutSlots_ClientId",
                table: "WorkoutSlots",
                newName: "IX_WorkoutSlots_UserId");

            migrationBuilder.RenameColumn(
                name: "TariffId",
                table: "Subscriptions",
                newName: "PlanId");

            migrationBuilder.RenameIndex(
                name: "IX_Subscriptions_TariffId",
                table: "Subscriptions",
                newName: "IX_Subscriptions_PlanId");

            migrationBuilder.RenameColumn(
                name: "PhotoUrl",
                table: "Products",
                newName: "Photo");

            migrationBuilder.AddColumn<string>(
                name: "ActivityName",
                table: "WorkoutSlots",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Tariffs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tariffs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rating",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "MetricLogs",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Tariffs_PlanId",
                table: "Subscriptions",
                column: "PlanId",
                principalTable: "Tariffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSlots_Users_UserId",
                table: "WorkoutSlots",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Tariffs_PlanId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSlots_Users_UserId",
                table: "WorkoutSlots");

            migrationBuilder.DropColumn(
                name: "ActivityName",
                table: "WorkoutSlots");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "MetricLogs");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "WorkoutSlots",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkoutSlots_UserId",
                table: "WorkoutSlots",
                newName: "IX_WorkoutSlots_ClientId");

            migrationBuilder.RenameColumn(
                name: "PlanId",
                table: "Subscriptions",
                newName: "TariffId");

            migrationBuilder.RenameIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                newName: "IX_Subscriptions_TariffId");

            migrationBuilder.RenameColumn(
                name: "Photo",
                table: "Products",
                newName: "PhotoUrl");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Tariffs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tariffs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Tariffs_TariffId",
                table: "Subscriptions",
                column: "TariffId",
                principalTable: "Tariffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSlots_Users_ClientId",
                table: "WorkoutSlots",
                column: "ClientId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
