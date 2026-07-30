using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YampiOperationalAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pending_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sku_id = table.Column<long>(type: "bigint", nullable: false),
                    sku_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sku_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    current_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    new_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    canceled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    executed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_action_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pending_actions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pending_actions_conversation_id_status",
                table: "pending_actions",
                columns: new[] { "conversation_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_pending_actions_active_conversation",
                table: "pending_actions",
                column: "conversation_id",
                unique: true,
                filter: "\"status\" IN ('PendingConfirmation', 'Confirmed', 'Executing')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pending_actions");
        }
    }
}
