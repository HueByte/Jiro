using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jiro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChatSessions_Clear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "ChatSessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "ChatSessions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
