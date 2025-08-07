using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jiro.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class ConfigureChatSessionMessageRelationship : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Messages_ChatSessions_ChatSessionId",
				table: "Messages");

			migrationBuilder.DropIndex(
				name: "IX_Messages_ChatSessionId",
				table: "Messages");

			migrationBuilder.DropColumn(
				name: "ChatSessionId",
				table: "Messages");

			migrationBuilder.CreateIndex(
				name: "IX_Messages_SessionId",
				table: "Messages",
				column: "SessionId");

			migrationBuilder.AddForeignKey(
				name: "FK_Messages_ChatSessions_SessionId",
				table: "Messages",
				column: "SessionId",
				principalTable: "ChatSessions",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Messages_ChatSessions_SessionId",
				table: "Messages");

			migrationBuilder.DropIndex(
				name: "IX_Messages_SessionId",
				table: "Messages");

			migrationBuilder.AddColumn<string>(
				name: "ChatSessionId",
				table: "Messages",
				type: "TEXT",
				nullable: true);

			migrationBuilder.CreateIndex(
				name: "IX_Messages_ChatSessionId",
				table: "Messages",
				column: "ChatSessionId");

			migrationBuilder.AddForeignKey(
				name: "FK_Messages_ChatSessions_ChatSessionId",
				table: "Messages",
				column: "ChatSessionId",
				principalTable: "ChatSessions",
				principalColumn: "Id");
		}
	}
}
