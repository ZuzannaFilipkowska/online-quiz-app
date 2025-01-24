using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrpcServer.Migrations
{
    /// <inheritdoc />
    public partial class AddHasAnswered : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAnswered",
                table: "Players",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAnswered",
                table: "Players");
        }
    }
}
