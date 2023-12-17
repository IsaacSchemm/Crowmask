using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crowmask.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Followers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Inbox = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SharedInbox = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Followers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboundActivities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Inbox = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JsonBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrivateAnnouncements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnouncedObjectId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateAnnouncements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    SubmitId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FriendsOnly = table.Column<bool>(type: "bit", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RatingId = table.Column<int>(type: "int", nullable: false),
                    SubtypeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CacheRefreshAttemptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CacheRefreshSucceededAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.SubmitId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfileText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CacheRefreshAttemptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CacheRefreshSucceededAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionMedia",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmissionSubmitId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionMedia_Submissions_SubmissionSubmitId",
                        column: x => x.SubmissionSubmitId,
                        principalTable: "Submissions",
                        principalColumn: "SubmitId");
                });

            migrationBuilder.CreateTable(
                name: "SubmissionTag",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tag = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmissionSubmitId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionTag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionTag_Submissions_SubmissionSubmitId",
                        column: x => x.SubmissionSubmitId,
                        principalTable: "Submissions",
                        principalColumn: "SubmitId");
                });

            migrationBuilder.CreateTable(
                name: "UserAvatar",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MediaId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAvatar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAvatar_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "UserLink",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Site = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsernameOrUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLink_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionMedia_SubmissionSubmitId",
                table: "SubmissionMedia",
                column: "SubmissionSubmitId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionTag_SubmissionSubmitId",
                table: "SubmissionTag",
                column: "SubmissionSubmitId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatar_UserId",
                table: "UserAvatar",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLink_UserId",
                table: "UserLink",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Followers");

            migrationBuilder.DropTable(
                name: "OutboundActivities");

            migrationBuilder.DropTable(
                name: "PrivateAnnouncements");

            migrationBuilder.DropTable(
                name: "SubmissionMedia");

            migrationBuilder.DropTable(
                name: "SubmissionTag");

            migrationBuilder.DropTable(
                name: "UserAvatar");

            migrationBuilder.DropTable(
                name: "UserLink");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
