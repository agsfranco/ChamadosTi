using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChamadosTi.Data
{
    /// <inheritdoc />
    public partial class TicketAutoReferenciado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TicketOrigemId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketOrigemId",
                table: "Tickets",
                column: "TicketOrigemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Tickets_TicketOrigemId",
                table: "Tickets",
                column: "TicketOrigemId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Tickets_TicketOrigemId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_TicketOrigemId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TicketOrigemId",
                table: "Tickets");
        }
    }
}
