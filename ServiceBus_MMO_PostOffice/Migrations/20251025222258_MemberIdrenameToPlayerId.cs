using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBus_MMO_PostOffice.Migrations
{
    /// <inheritdoc />
    public partial class MemberIdrenameToPlayerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MemberId",
                table: "ScheduledMessage",
                newName: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "ScheduledMessage",
                newName: "MemberId");
        }
    }
}
