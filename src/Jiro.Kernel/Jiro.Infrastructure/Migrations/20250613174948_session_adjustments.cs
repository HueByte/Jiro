using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jiro.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class session_adjustments : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Messages_ChatSessions_ChatSessionId",
				table: "Messages");

			migrationBuilder.RenameColumn(
				name: "Role",
				table: "Messages",
				newName: "SessionId");

			migrationBuilder.RenameColumn(
				name: "UserId",
				table: "ChatSessions",
				newName: "Name");

			migrationBuilder.AlterColumn<string>(
				name: "ChatSessionId",
				table: "Messages",
				type: "TEXT",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "TEXT");

			migrationBuilder.AddColumn<DateTime>(
				name: "CreatedAt",
				table: "Messages",
				type: "TEXT",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

			migrationBuilder.AddColumn<string>(
				name: "InstanceId",
				table: "Messages",
				type: "TEXT",
				nullable: false,
				defaultValue: "");

			migrationBuilder.AddColumn<bool>(
				name: "IsUser",
				table: "Messages",
				type: "INTEGER",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AddColumn<int>(
				name: "Type",
				table: "Messages",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<DateTime>(
				name: "CreatedAt",
				table: "ChatSessions",
				type: "TEXT",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

			migrationBuilder.AddColumn<string>(
				name: "Description",
				table: "ChatSessions",
				type: "TEXT",
				nullable: false,
				defaultValue: "");

			migrationBuilder.AddColumn<DateTime>(
				name: "LastUpdatedAt",
				table: "ChatSessions",
				type: "TEXT",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

			migrationBuilder.AddForeignKey(
				name: "FK_Messages_ChatSessions_ChatSessionId",
				table: "Messages",
				column: "ChatSessionId",
				principalTable: "ChatSessions",
				principalColumn: "Id");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Messages_ChatSessions_ChatSessionId",
				table: "Messages");

			migrationBuilder.DropColumn(
				name: "CreatedAt",
				table: "Messages");

			migrationBuilder.DropColumn(
				name: "InstanceId",
				table: "Messages");

			migrationBuilder.DropColumn(
				name: "IsUser",
				table: "Messages");

			migrationBuilder.DropColumn(
				name: "Type",
				table: "Messages");

			migrationBuilder.DropColumn(
				name: "CreatedAt",
				table: "ChatSessions");

			migrationBuilder.DropColumn(
				name: "Description",
				table: "ChatSessions");

			migrationBuilder.DropColumn(
				name: "LastUpdatedAt",
				table: "ChatSessions");

			migrationBuilder.RenameColumn(
				name: "SessionId",
				table: "Messages",
				newName: "Role");

			migrationBuilder.RenameColumn(
				name: "Name",
				table: "ChatSessions",
				newName: "UserId");

			migrationBuilder.AlterColumn<string>(
				name: "ChatSessionId",
				table: "Messages",
				type: "TEXT",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "TEXT",
				oldNullable: true);

			migrationBuilder.AddForeignKey(
				name: "FK_Messages_ChatSessions_ChatSessionId",
				table: "Messages",
				column: "ChatSessionId",
				principalTable: "ChatSessions",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}
	}
}
