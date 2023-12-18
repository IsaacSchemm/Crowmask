using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crowmask.Data.Migrations
{
    public partial class SubmissionUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Link",
                table: "Submissions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Link",
                table: "Submissions");
        }
    }
}
