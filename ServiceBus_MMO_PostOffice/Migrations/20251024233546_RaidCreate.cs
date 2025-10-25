using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBus_MMO_PostOffice.Migrations
{
    /// <inheritdoc />
    public partial class RaidCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Raid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuildId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Raid", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Raid_Guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guild",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidParticipant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RaidId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidParticipant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidParticipant_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidParticipant_Raid_RaidId",
                        column: x => x.RaidId,
                        principalTable: "Raid",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Raid_GuildId",
                table: "Raid",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidParticipant_PlayerId",
                table: "RaidParticipant",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidParticipant_RaidId",
                table: "RaidParticipant",
                column: "RaidId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaidParticipant");

            migrationBuilder.DropTable(
                name: "Raid");
        }
    }
}
