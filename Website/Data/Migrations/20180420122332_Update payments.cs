using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MyVoltage.Data.Migrations
{
    public partial class Updatepayments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionAccepted",
                table: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatusID",
                table: "Payments",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentStatusID",
                table: "Payments",
                column: "PaymentStatusID");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentStatuses_PaymentStatusID",
                table: "Payments",
                column: "PaymentStatusID",
                principalTable: "PaymentStatuses",
                principalColumn: "PaymentStatusID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentStatuses_PaymentStatusID",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentStatusID",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentStatusID",
                table: "Payments");

            migrationBuilder.AddColumn<string>(
                name: "TransactionAccepted",
                table: "Payments",
                nullable: true);
        }
    }
}
