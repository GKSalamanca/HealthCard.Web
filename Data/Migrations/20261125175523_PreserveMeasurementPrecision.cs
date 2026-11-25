using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class PreserveMeasurementPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "CustomNormMin",
                table: "UserParameterNorms",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomNormMax",
                table: "UserParameterNorms",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "TestResults",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "NormMin",
                table: "TestParameters",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "NormMax",
                table: "TestParameters",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "CustomNormMin",
                table: "UserParameterNorms",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomNormMax",
                table: "UserParameterNorms",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "TestResults",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "NormMin",
                table: "TestParameters",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "NormMax",
                table: "TestParameters",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");
        }
    }
}
