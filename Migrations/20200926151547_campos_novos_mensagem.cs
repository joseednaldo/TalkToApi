using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TalkToApi.Migrations
{
    public partial class campos_novos_mensagem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           
            migrationBuilder.AddColumn<DateTime>(
                name: "Atualizado",
                table: "Mensagem",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Excluido",
                table: "Mensagem",
                nullable: false,
                defaultValue: false);

           
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mensagem_AspNetUsers_DeId",
                table: "Mensagem");

            migrationBuilder.DropForeignKey(
                name: "FK_Mensagem_AspNetUsers_ParaId",
                table: "Mensagem");

            migrationBuilder.DropColumn(
                name: "Atualizado",
                table: "Mensagem");

            migrationBuilder.DropColumn(
                name: "Excluido",
                table: "Mensagem");

            migrationBuilder.AlterColumn<string>(
                name: "Texto",
                table: "Mensagem",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "ParaId",
                table: "Mensagem",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "DeId",
                table: "Mensagem",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddForeignKey(
                name: "FK_Mensagem_AspNetUsers_DeId",
                table: "Mensagem",
                column: "DeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mensagem_AspNetUsers_ParaId",
                table: "Mensagem",
                column: "ParaId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
