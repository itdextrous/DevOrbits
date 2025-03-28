using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MyVoltage.Data.Migrations
{
    public partial class UpdatePaymentMethod : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentMethods_PaymentMethodID",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentStatuses_PaymentStatusID",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "PaymentStatusID",
                table: "Payments",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "PaymentMethodID",
                table: "Payments",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentMethods_PaymentMethodID",
                table: "Payments",
                column: "PaymentMethodID",
                principalTable: "PaymentMethods",
                principalColumn: "PaymentMethodID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentStatuses_PaymentStatusID",
                table: "Payments",
                column: "PaymentStatusID",
                principalTable: "PaymentStatuses",
                principalColumn: "PaymentStatusID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentMethods_PaymentMethodID",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentStatuses_PaymentStatusID",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "PaymentStatusID",
                table: "Payments",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentMethodID",
                table: "Payments",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentMethods_PaymentMethodID",
                table: "Payments",
                column: "PaymentMethodID",
                principalTable: "PaymentMethods",
                principalColumn: "PaymentMethodID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentStatuses_PaymentStatusID",
                table: "Payments",
                column: "PaymentStatusID",
                principalTable: "PaymentStatuses",
                principalColumn: "PaymentStatusID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
