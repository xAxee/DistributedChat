using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistributedChat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateRoomsAndManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_messages_rooms_room_id",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "fk_room_members_rooms_room_id",
                table: "room_members");

            migrationBuilder.AddColumn<string>(
                name: "invite_token_hash",
                table: "rooms",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_private",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "rooms",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_rooms_invite_token_hash",
                table: "rooms",
                column: "invite_token_hash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_messages_rooms_room_id",
                table: "messages",
                column: "room_id",
                principalTable: "rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_room_members_rooms_room_id",
                table: "room_members",
                column: "room_id",
                principalTable: "rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_messages_rooms_room_id",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "fk_room_members_rooms_room_id",
                table: "room_members");

            migrationBuilder.DropIndex(
                name: "ux_rooms_invite_token_hash",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "invite_token_hash",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "is_private",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "rooms");

            migrationBuilder.AddForeignKey(
                name: "fk_messages_rooms_room_id",
                table: "messages",
                column: "room_id",
                principalTable: "rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_room_members_rooms_room_id",
                table: "room_members",
                column: "room_id",
                principalTable: "rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
