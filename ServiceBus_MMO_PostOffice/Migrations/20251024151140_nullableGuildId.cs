using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBus_MMO_PostOffice.Migrations
{
    /// <inheritdoc />
    public partial class nullableGuildId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Player_Guild_GuildId",
                table: "Player");

            migrationBuilder.AlterColumn<int>(
                name: "GuildId",
                table: "Player",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Player_Guild_GuildId",
                table: "Player",
                column: "GuildId",
                principalTable: "Guild",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Player_Guild_GuildId",
                table: "Player");

            migrationBuilder.AlterColumn<int>(
                name: "GuildId",
                table: "Player",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Player_Guild_GuildId",
                table: "Player",
                column: "GuildId",
                principalTable: "Guild",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
