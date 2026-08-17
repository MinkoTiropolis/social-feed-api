using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialFeed.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Posts_DeletedAt_CreatedAt_Id",
                table: "Posts",
                columns: new[] { "DeletedAt", "CreatedAt", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_DeletedAt_CreatedAt_Id",
                table: "Posts");
        }
    }
}
