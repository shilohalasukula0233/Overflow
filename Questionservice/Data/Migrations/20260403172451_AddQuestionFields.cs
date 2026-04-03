using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Questionservice.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "viewCount",
                table: "Questions",
                newName: "ViewCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ViewCount",
                table: "Questions",
                newName: "viewCount");
        }
    }
}
