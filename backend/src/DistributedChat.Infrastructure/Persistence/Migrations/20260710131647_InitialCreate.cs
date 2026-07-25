using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistributedChat.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    private static readonly string[] MessageHistoryIndexColumns =
    [
        "room_id",
        "created_at",
        "id",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "processed_events",
            columns: table => new
            {
                consumer_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_processed_events", x => new { x.consumer_id, x.event_id });
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                username = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                normalized_username = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "rooms",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_rooms", x => x.id);
                table.ForeignKey(
                    name: "fk_rooms_users_created_by_user_id",
                    column: x => x.created_by_user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                room_id = table.Column<Guid>(type: "uuid", nullable: false),
                sender_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_messages", x => x.id);
                table.ForeignKey(
                    name: "fk_messages_rooms_room_id",
                    column: x => x.room_id,
                    principalTable: "rooms",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_messages_users_sender_user_id",
                    column: x => x.sender_user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "room_members",
            columns: table => new
            {
                room_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_room_members", x => new { x.room_id, x.user_id });
                table.ForeignKey(
                    name: "fk_room_members_rooms_room_id",
                    column: x => x.room_id,
                    principalTable: "rooms",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_room_members_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_messages_room_id_created_at_id",
            table: "messages",
            columns: MessageHistoryIndexColumns);

        migrationBuilder.CreateIndex(
            name: "IX_messages_sender_user_id",
            table: "messages",
            column: "sender_user_id");

        migrationBuilder.CreateIndex(
            name: "ix_processed_events_processed_at",
            table: "processed_events",
            column: "processed_at");

        migrationBuilder.CreateIndex(
            name: "ix_room_members_user_id",
            table: "room_members",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_rooms_created_at",
            table: "rooms",
            column: "created_at");

        migrationBuilder.CreateIndex(
            name: "IX_rooms_created_by_user_id",
            table: "rooms",
            column: "created_by_user_id");

        migrationBuilder.CreateIndex(
            name: "ux_users_normalized_email",
            table: "users",
            column: "normalized_email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_users_normalized_username",
            table: "users",
            column: "normalized_username",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "messages");

        migrationBuilder.DropTable(
            name: "processed_events");

        migrationBuilder.DropTable(
            name: "room_members");

        migrationBuilder.DropTable(
            name: "rooms");

        migrationBuilder.DropTable(
            name: "users");
    }
}
