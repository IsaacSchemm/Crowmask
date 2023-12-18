using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crowmask.Data.Migrations
{
    public partial class Inbox : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Sent",
                table: "PrivateAnnouncements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FollowActivityId",
                table: "Followers",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sent",
                table: "PrivateAnnouncements");

            migrationBuilder.DropColumn(
                name: "FollowActivityId",
                table: "Followers");
        }
    }
}
