using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayaAutos.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCI_RUCNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clientes_CI_RUC",
                table: "Clientes");

            migrationBuilder.AlterColumn<string>(
                name: "CI_RUC",
                table: "Clientes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_CI_RUC",
                table: "Clientes",
                column: "CI_RUC",
                unique: true,
                filter: "[CI_RUC] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clientes_CI_RUC",
                table: "Clientes");

            migrationBuilder.AlterColumn<string>(
                name: "CI_RUC",
                table: "Clientes",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_CI_RUC",
                table: "Clientes",
                column: "CI_RUC",
                unique: true);
        }
    }
}
