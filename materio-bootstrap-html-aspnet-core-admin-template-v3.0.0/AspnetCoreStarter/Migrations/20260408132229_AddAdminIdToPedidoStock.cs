using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminIdToPedidoStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /* 
            Migration commented out because the database is already in the target state 
            but the migration was not marked as completed. This allows EF to continue 
            without errors while preserving existing data.
            
            [... original migration code hidden ...]
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_escolas_agrupamentos_id_agrupamento",
                table: "escolas");

            migrationBuilder.DropForeignKey(
                name: "FK_utilizadores_empresas_id_empresa",
                table: "utilizadores");

            migrationBuilder.DropTable(
                name: "componentes_equipamentos");

            migrationBuilder.DropTable(
                name: "contratos");

            migrationBuilder.DropTable(
                name: "coordenadores");

            migrationBuilder.DropTable(
                name: "diretores");

            migrationBuilder.DropTable(
                name: "emprestimos");

            migrationBuilder.DropTable(
                name: "historico");

            migrationBuilder.DropTable(
                name: "mensagens");

            migrationBuilder.DropTable(
                name: "pedidos_stock");

            migrationBuilder.DropTable(
                name: "reparos");

            migrationBuilder.DropTable(
                name: "status_equipamento");

            migrationBuilder.DropTable(
                name: "stock_empresa");

            migrationBuilder.DropTable(
                name: "stock_tecnico");

            migrationBuilder.DropTable(
                name: "tickets");

            migrationBuilder.DropTable(
                name: "administradores");

            migrationBuilder.DropTable(
                name: "tecnicos");

            migrationBuilder.DropTable(
                name: "equipamentos");

            migrationBuilder.DropTable(
                name: "agrupamentos");

            migrationBuilder.DropTable(
                name: "empresas");

            migrationBuilder.DropTable(
                name: "salas");

            migrationBuilder.DropTable(
                name: "professores");

            migrationBuilder.DropTable(
                name: "blocos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_utilizadores",
                table: "utilizadores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_escolas",
                table: "escolas");

            migrationBuilder.DropIndex(
                name: "IX_escolas_id_agrupamento",
                table: "escolas");

            migrationBuilder.DropColumn(
                name: "palavra_passe",
                table: "utilizadores");

            migrationBuilder.DropColumn(
                name: "status_conta",
                table: "utilizadores");

            migrationBuilder.DropColumn(
                name: "id_agrupamento",
                table: "escolas");

            migrationBuilder.RenameTable(
                name: "utilizadores",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "escolas",
                newName: "Schools");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "reset_token_expiry",
                table: "Users",
                newName: "ResetTokenExpiry");

            migrationBuilder.RenameColumn(
                name: "profile_photo_path",
                table: "Users",
                newName: "ProfilePhotoPath");

            migrationBuilder.RenameColumn(
                name: "password_reset_token",
                table: "Users",
                newName: "PasswordResetToken");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "Users",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "data_criacao",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "id_utilizador",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id_empresa",
                table: "Users",
                newName: "SchoolId");

            migrationBuilder.RenameIndex(
                name: "IX_utilizadores_id_empresa",
                table: "Users",
                newName: "IX_Users_SchoolId");

            migrationBuilder.RenameColumn(
                name: "nome_escola",
                table: "Schools",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "localizacao",
                table: "Schools",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "id_escola",
                table: "Schools",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Grouping",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Schools",
                keyColumn: "Name",
                keyValue: null,
                column: "Name",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Schools",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Schools",
                keyColumn: "Address",
                keyValue: null,
                column: "Address",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Schools",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Schools",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Grouping",
                table: "Schools",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Schools",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "Schools",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Schools",
                table: "Schools",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Schools_SchoolId",
                table: "Users",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id");
        }
    }
}
