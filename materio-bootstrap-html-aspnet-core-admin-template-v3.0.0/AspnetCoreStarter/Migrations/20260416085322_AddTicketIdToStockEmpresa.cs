using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreStarter.Migrations
{
    public partial class AddTicketIdToStockEmpresa : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_ticket",
                table: "stock_empresa",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_empresa_id_ticket",
                table: "stock_empresa",
                column: "id_ticket");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_empresa_tickets_id_ticket",
                table: "stock_empresa",
                column: "id_ticket",
                principalTable: "tickets",
                principalColumn: "id_ticket");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_empresa_tickets_id_ticket",
                table: "stock_empresa");

            migrationBuilder.DropIndex(
                name: "IX_stock_empresa_id_ticket",
                table: "stock_empresa");

            migrationBuilder.DropColumn(
                name: "id_ticket",
                table: "stock_empresa");
        }
    }
}
