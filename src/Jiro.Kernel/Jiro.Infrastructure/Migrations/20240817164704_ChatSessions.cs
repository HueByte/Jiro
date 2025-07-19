using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jiro.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class ChatSessions : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "ChatSessions",
				columns: static table => new
				{
					Id = table.Column<string>(type: "TEXT", nullable: false),
					SessionId = table.Column<string>(type: "TEXT", nullable: false),
					UserId = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: static table =>
				{
					table.PrimaryKey("PK_ChatSessions", static x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Messages",
				columns: static table => new
				{
					Id = table.Column<string>(type: "TEXT", nullable: false),
					Role = table.Column<string>(type: "TEXT", nullable: false),
					Content = table.Column<string>(type: "TEXT", nullable: false),
					ChatSessionId = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: static table =>
				{
					table.PrimaryKey("PK_Messages", static x => x.Id);
					table.ForeignKey(
						name: "FK_Messages_ChatSessions_ChatSessionId",
						column: static x => x.ChatSessionId,
						principalTable: "ChatSessions",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Messages_ChatSessionId",
				table: "Messages",
				column: "ChatSessionId");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Messages");

			migrationBuilder.DropTable(
				name: "ChatSessions");
		}
	}
}
