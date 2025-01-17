using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrpcServer.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answers_Questions_QuestionId",
                table: "Answers");

            migrationBuilder.DropIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers");

            migrationBuilder.AddColumn<string>(
                name: "DbQuestionId",
                table: "Answers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Answers_DbQuestionId",
                table: "Answers",
                column: "DbQuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_Questions_DbQuestionId",
                table: "Answers",
                column: "DbQuestionId",
                principalTable: "Questions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answers_Questions_DbQuestionId",
                table: "Answers");

            migrationBuilder.DropIndex(
                name: "IX_Answers_DbQuestionId",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "DbQuestionId",
                table: "Answers");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_Questions_QuestionId",
                table: "Answers",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
