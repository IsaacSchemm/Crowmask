using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crowmask.Data.Migrations
{
    public partial class FollowerUpdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FollowActivityId",
                table: "Followers",
                newName: "FollowId");

            migrationBuilder.AddColumn<string>(
                name: "ActorId",
                table: "Followers",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "Followers");

            migrationBuilder.RenameColumn(
                name: "FollowId",
                table: "Followers",
                newName: "FollowActivityId");
        }
    }
}
