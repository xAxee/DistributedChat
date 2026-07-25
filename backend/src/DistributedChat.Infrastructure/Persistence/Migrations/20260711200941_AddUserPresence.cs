using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistributedChat.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddUserPresence : Migration
{
    private static readonly string[] UserPresenceActiveHeartbeatIndexColumns =
    [
        "user_id",
        "connection_count",
        "last_heartbeat_at",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "user_presences",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                instance_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                connection_count = table.Column<int>(type: "integer", nullable: false),
                last_heartbeat_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_presences", x => new { x.user_id, x.instance_id });
                table.ForeignKey(
                    name: "fk_user_presences_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_user_presences_instance_id",
            table: "user_presences",
            column: "instance_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_presences_user_id_active_heartbeat",
            table: "user_presences",
            columns: UserPresenceActiveHeartbeatIndexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "user_presences");
    }
}
