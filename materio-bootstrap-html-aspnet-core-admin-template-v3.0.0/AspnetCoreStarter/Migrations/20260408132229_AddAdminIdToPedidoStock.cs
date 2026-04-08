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
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Schools_SchoolId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Schools",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "Grouping",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "Grouping",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "Schools");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "utilizadores");

            migrationBuilder.RenameTable(
                name: "Schools",
                newName: "escolas");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "utilizadores",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "utilizadores",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "ResetTokenExpiry",
                table: "utilizadores",
                newName: "reset_token_expiry");

            migrationBuilder.RenameColumn(
                name: "ProfilePhotoPath",
                table: "utilizadores",
                newName: "profile_photo_path");

            migrationBuilder.RenameColumn(
                name: "PasswordResetToken",
                table: "utilizadores",
                newName: "password_reset_token");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "utilizadores",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "utilizadores",
                newName: "data_criacao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "utilizadores",
                newName: "id_utilizador");

            migrationBuilder.RenameColumn(
                name: "SchoolId",
                table: "utilizadores",
                newName: "id_empresa");

            migrationBuilder.RenameIndex(
                name: "IX_Users_SchoolId",
                table: "utilizadores",
                newName: "IX_utilizadores_id_empresa");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "escolas",
                newName: "nome_escola");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "escolas",
                newName: "localizacao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "escolas",
                newName: "id_escola");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "utilizadores",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "palavra_passe",
                table: "utilizadores",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "status_conta",
                table: "utilizadores",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "nome_escola",
                table: "escolas",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "localizacao",
                table: "escolas",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "id_agrupamento",
                table: "escolas",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_utilizadores",
                table: "utilizadores",
                column: "id_utilizador");

            migrationBuilder.AddPrimaryKey(
                name: "PK_escolas",
                table: "escolas",
                column: "id_escola");

            migrationBuilder.CreateTable(
                name: "agrupamentos",
                columns: table => new
                {
                    id_agrupamento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome_agrupamento = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agrupamentos", x => x.id_agrupamento);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "blocos",
                columns: table => new
                {
                    id_bloco = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome_bloco = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_escola = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocos", x => x.id_bloco);
                    table.ForeignKey(
                        name: "FK_blocos_escolas_id_escola",
                        column: x => x.id_escola,
                        principalTable: "escolas",
                        principalColumn: "id_escola",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "coordenadores",
                columns: table => new
                {
                    id_utilizador = table.Column<int>(type: "int", nullable: false),
                    id_escola = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coordenadores", x => x.id_utilizador);
                    table.ForeignKey(
                        name: "FK_coordenadores_escolas_id_escola",
                        column: x => x.id_escola,
                        principalTable: "escolas",
                        principalColumn: "id_escola");
                    table.ForeignKey(
                        name: "FK_coordenadores_utilizadores_id_utilizador",
                        column: x => x.id_utilizador,
                        principalTable: "utilizadores",
                        principalColumn: "id_utilizador",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    id_empresa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome_empresa = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    localizacao = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresas", x => x.id_empresa);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mensagens",
                columns: table => new
                {
                    id_mensagem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_remetente = table.Column<int>(type: "int", nullable: false),
                    id_destinatario = table.Column<int>(type: "int", nullable: false),
                    conteudo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lida = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    data_envio = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mensagens", x => x.id_mensagem);
                    table.ForeignKey(
                        name: "FK_mensagens_utilizadores_id_destinatario",
                        column: x => x.id_destinatario,
                        principalTable: "utilizadores",
                        principalColumn: "id_utilizador",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mensagens_utilizadores_id_remetente",
                        column: x => x.id_remetente,
                        principalTable: "utilizadores",
                        principalColumn: "id_utilizador",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "stock_tecnico",
                columns: table => new
                {
                    id_stock_tecnico = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome_equipamento = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descricao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    disponivel = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    id_tecnico = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_tecnico", x => x.id_stock_tecnico);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tecnicos",
                columns: table => new
                {
                    id_utilizador = table.Column<int>(type: "int", nullable: false),
                    area_tecnica = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nivel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tecnicos", x => x.id_utilizador);
                    table.ForeignKey(
                        name: "FK_tecnicos_utilizadores_id_utilizador",
                        column: x => x.id_utilizador,
                        principalTable: "utilizadores",
                        principalColumn: "id_utilizador",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "administradores",
                columns: table => new
                {
                    id_utilizador = table.Column<int>(type: "int", nullable: false),
                    id_agrupamento = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administradores", x => x.id_utilizador);
                    table.ForeignKey(
                        name: "FK_administradores_agrupamentos_id_agrupamento",
                        column: x => x.id_agrupamento,
                        principalTable: "agrupamentos",
                        principalColumn: "id_agrupamento");
                    table.ForeignKey(
                        name: "FK_administradores_utilizadores_id_utilizador",
                        column: x => x.id_utilizador,
                        principalTable: "utilizadores",
                        principalColumn: "id_utilizador",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "diretores",
                columns: table => new
                {
                    id_utilizador = table.Column<int>(type: "int", nullable: false),
                    id_agrupamento = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diretores", x => x.id_utilizador);
                    table.ForeignKey(
                        name: "FK_diretores_agrupamentos_id_agrupamento",
                        column: x => x.id_agrupamento,
                        principalTable: "agrupamentos",
                        principalColumn: "id_agrupamento");
                    table.ForeignKey(
                        name: "FK_diretores_utilizadores_id_utilizador",
                        column: x => x.id_utilizador,
                        principalTable: "utilizadores",
                        principalColumn: "id_utilizador",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pedidos_stock",
                columns: table => new
                {
                    id_pedido = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome_artigo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tipo_artigo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quantidade = table.Column<int>(type: "int", nullable: false),
                    notas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_coordenador = table.Column<int>(type: "int", nullable: true),
                    id_escola = table.Column<int>(type: "int", nullable: true),
                    id_agrupamento = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notas_diretor = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_admin = table.Column<int>(type: "int", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedidos_stock", x => x.id_pedido);
                    table.ForeignKey(
                        name: "FK_pedidos_stock_agrupamentos_id_agrupamento",
                        column: x => x.id_agrupamento,
                        principalTable: "agrupamentos",
                        principalColumn: "id_agrupamento");
                    table.ForeignKey(
                        name: "FK_pedidos_stock_escolas_id_escola",
                        column: x => x.id_escola,
                        principalTable: "escolas",
                        principalColumn: "id_escola");
                    table.ForeignKey(
                        name: "FK_pedidos_stock_utilizadores_id_admin",
                        column: x => x.id_admin,
                        principalTable: "utilizadores",
                        principalColumn: "id_utilizador");
                    table.ForeignKey(
                        name: "FK_pedidos_stock_utilizadores_id_coordenador",
                        column: x => x.id_coordenador,
                        principalTable: "utilizadores",
                        principalColumn: "id_utilizador");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "professores",
                columns: table => new
                {
                    id_utilizador = table.Column<int>(type: "int", nullable: false),
                    id_bloco = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professores", x => x.id_utilizador);
                    table.ForeignKey(
                        name: "FK_professores_blocos_id_bloco",
                        column: x => x.id_bloco,
                        principalTable: "blocos",
                        principalColumn: "id_bloco");
                    table.ForeignKey(
                        name: "FK_professores_utilizadores_id_utilizador",
                        column: x => x.id_utilizador,
                        principalTable: "utilizadores",
                        principalColumn: "id_utilizador",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "contratos",
                columns: table => new
                {
                    id_contrato = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    periodo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tipo_contrato = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status_contrato = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descricao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nivel_urgencia = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_agrupamento = table.Column<int>(type: "int", nullable: true),
                    id_escola = table.Column<int>(type: "int", nullable: true),
                    id_empresa = table.Column<int>(type: "int", nullable: true),
                    id_admin = table.Column<int>(type: "int", nullable: true),
                    data_expiracao = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contratos", x => x.id_contrato);
                    table.ForeignKey(
                        name: "FK_contratos_administradores_id_admin",
                        column: x => x.id_admin,
                        principalTable: "administradores",
                        principalColumn: "id_utilizador");
                    table.ForeignKey(
                        name: "FK_contratos_agrupamentos_id_agrupamento",
                        column: x => x.id_agrupamento,
                        principalTable: "agrupamentos",
                        principalColumn: "id_agrupamento");
                    table.ForeignKey(
                        name: "FK_contratos_empresas_id_empresa",
                        column: x => x.id_empresa,
                        principalTable: "empresas",
                        principalColumn: "id_empresa");
                    table.ForeignKey(
                        name: "FK_contratos_escolas_id_escola",
                        column: x => x.id_escola,
                        principalTable: "escolas",
                        principalColumn: "id_escola");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "stock_empresa",
                columns: table => new
                {
                    id_stock = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome_equipamento = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tipo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descricao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    disponivel = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_tecnico = table.Column<int>(type: "int", nullable: true),
                    id_agrupamento = table.Column<int>(type: "int", nullable: true),
                    id_escola = table.Column<int>(type: "int", nullable: true),
                    id_admin = table.Column<int>(type: "int", nullable: true),
                    id_empresa = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_empresa", x => x.id_stock);
                    table.ForeignKey(
                        name: "FK_stock_empresa_administradores_id_admin",
                        column: x => x.id_admin,
                        principalTable: "administradores",
                        principalColumn: "id_utilizador");
                    table.ForeignKey(
                        name: "FK_stock_empresa_agrupamentos_id_agrupamento",
                        column: x => x.id_agrupamento,
                        principalTable: "agrupamentos",
                        principalColumn: "id_agrupamento");
                    table.ForeignKey(
                        name: "FK_stock_empresa_empresas_id_empresa",
                        column: x => x.id_empresa,
                        principalTable: "empresas",
                        principalColumn: "id_empresa");
                    table.ForeignKey(
                        name: "FK_stock_empresa_escolas_id_escola",
                        column: x => x.id_escola,
                        principalTable: "escolas",
                        principalColumn: "id_escola");
                    table.ForeignKey(
                        name: "FK_stock_empresa_tecnicos_id_tecnico",
                        column: x => x.id_tecnico,
                        principalTable: "tecnicos",
                        principalColumn: "id_utilizador");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "salas",
                columns: table => new
                {
                    id_sala = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome_sala = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_bloco = table.Column<int>(type: "int", nullable: false),
                    id_professor_responsavel = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salas", x => x.id_sala);
                    table.ForeignKey(
                        name: "FK_salas_blocos_id_bloco",
                        column: x => x.id_bloco,
                        principalTable: "blocos",
                        principalColumn: "id_bloco",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_salas_professores_id_professor_responsavel",
                        column: x => x.id_professor_responsavel,
                        principalTable: "professores",
                        principalColumn: "id_utilizador");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "equipamentos",
                columns: table => new
                {
                    id_equipamento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome_equipamento = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tipo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    marca = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modelo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    numero_patrimonio = table.Column<long>(type: "bigint", nullable: true),
                    numero_serie = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mac_address = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_sala = table.Column<int>(type: "int", nullable: true),
                    id_empresa = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipamentos", x => x.id_equipamento);
                    table.ForeignKey(
                        name: "FK_equipamentos_empresas_id_empresa",
                        column: x => x.id_empresa,
                        principalTable: "empresas",
                        principalColumn: "id_empresa");
                    table.ForeignKey(
                        name: "FK_equipamentos_salas_id_sala",
                        column: x => x.id_sala,
                        principalTable: "salas",
                        principalColumn: "id_sala");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "componentes_equipamentos",
                columns: table => new
                {
                    id_componente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_equipamento = table.Column<int>(type: "int", nullable: false),
                    processador = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    memoria_ram = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    armazenamento = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    placa_grafica = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sistema_operativo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bateria = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cooler = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fonte_alimentacao = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    motherboard = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    observacoes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_componentes_equipamentos", x => x.id_componente);
                    table.ForeignKey(
                        name: "FK_componentes_equipamentos_equipamentos_id_equipamento",
                        column: x => x.id_equipamento,
                        principalTable: "equipamentos",
                        principalColumn: "id_equipamento",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "emprestimos",
                columns: table => new
                {
                    id_emprestimo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_agrupamento = table.Column<int>(type: "int", nullable: false),
                    data_emprestimo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    tipo_emprestimo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_equipamento = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emprestimos", x => x.id_emprestimo);
                    table.ForeignKey(
                        name: "FK_emprestimos_agrupamentos_id_agrupamento",
                        column: x => x.id_agrupamento,
                        principalTable: "agrupamentos",
                        principalColumn: "id_agrupamento",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_emprestimos_equipamentos_id_equipamento",
                        column: x => x.id_equipamento,
                        principalTable: "equipamentos",
                        principalColumn: "id_equipamento",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "historico",
                columns: table => new
                {
                    id_historico = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    data_mudanca = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    estado = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_equipamento = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico", x => x.id_historico);
                    table.ForeignKey(
                        name: "FK_historico_equipamentos_id_equipamento",
                        column: x => x.id_equipamento,
                        principalTable: "equipamentos",
                        principalColumn: "id_equipamento",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reparos",
                columns: table => new
                {
                    id_reparo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_equipamento = table.Column<int>(type: "int", nullable: false),
                    id_tecnico = table.Column<int>(type: "int", nullable: false),
                    descricao_avaria = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_reparo = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reparos", x => x.id_reparo);
                    table.ForeignKey(
                        name: "FK_reparos_equipamentos_id_equipamento",
                        column: x => x.id_equipamento,
                        principalTable: "equipamentos",
                        principalColumn: "id_equipamento",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reparos_tecnicos_id_tecnico",
                        column: x => x.id_tecnico,
                        principalTable: "tecnicos",
                        principalColumn: "id_utilizador",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "status_equipamento",
                columns: table => new
                {
                    id_status = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    estado = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    versao = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    empresa = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_equipamento = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_equipamento", x => x.id_status);
                    table.ForeignKey(
                        name: "FK_status_equipamento_equipamentos_id_equipamento",
                        column: x => x.id_equipamento,
                        principalTable: "equipamentos",
                        principalColumn: "id_equipamento");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    id_ticket = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nivel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descricao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    periodo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_escola = table.Column<int>(type: "int", nullable: true),
                    id_admin = table.Column<int>(type: "int", nullable: true),
                    id_tecnico = table.Column<int>(type: "int", nullable: true),
                    id_equipamento = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_criacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.id_ticket);
                    table.ForeignKey(
                        name: "FK_tickets_equipamentos_id_equipamento",
                        column: x => x.id_equipamento,
                        principalTable: "equipamentos",
                        principalColumn: "id_equipamento");
                    table.ForeignKey(
                        name: "FK_tickets_escolas_id_escola",
                        column: x => x.id_escola,
                        principalTable: "escolas",
                        principalColumn: "id_escola");
                    table.ForeignKey(
                        name: "FK_tickets_utilizadores_id_admin",
                        column: x => x.id_admin,
                        principalTable: "utilizadores",
                        principalColumn: "id_utilizador");
                    table.ForeignKey(
                        name: "FK_tickets_utilizadores_id_tecnico",
                        column: x => x.id_tecnico,
                        principalTable: "utilizadores",
                        principalColumn: "id_utilizador");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_escolas_id_agrupamento",
                table: "escolas",
                column: "id_agrupamento");

            migrationBuilder.CreateIndex(
                name: "IX_administradores_id_agrupamento",
                table: "administradores",
                column: "id_agrupamento");

            migrationBuilder.CreateIndex(
                name: "IX_blocos_id_escola",
                table: "blocos",
                column: "id_escola");

            migrationBuilder.CreateIndex(
                name: "IX_componentes_equipamentos_id_equipamento",
                table: "componentes_equipamentos",
                column: "id_equipamento");

            migrationBuilder.CreateIndex(
                name: "IX_contratos_id_admin",
                table: "contratos",
                column: "id_admin");

            migrationBuilder.CreateIndex(
                name: "IX_contratos_id_agrupamento",
                table: "contratos",
                column: "id_agrupamento");

            migrationBuilder.CreateIndex(
                name: "IX_contratos_id_empresa",
                table: "contratos",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_contratos_id_escola",
                table: "contratos",
                column: "id_escola");

            migrationBuilder.CreateIndex(
                name: "IX_coordenadores_id_escola",
                table: "coordenadores",
                column: "id_escola");

            migrationBuilder.CreateIndex(
                name: "IX_diretores_id_agrupamento",
                table: "diretores",
                column: "id_agrupamento");

            migrationBuilder.CreateIndex(
                name: "IX_emprestimos_id_agrupamento",
                table: "emprestimos",
                column: "id_agrupamento");

            migrationBuilder.CreateIndex(
                name: "IX_emprestimos_id_equipamento",
                table: "emprestimos",
                column: "id_equipamento");

            migrationBuilder.CreateIndex(
                name: "IX_equipamentos_id_empresa",
                table: "equipamentos",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_equipamentos_id_sala",
                table: "equipamentos",
                column: "id_sala");

            migrationBuilder.CreateIndex(
                name: "IX_historico_id_equipamento",
                table: "historico",
                column: "id_equipamento");

            migrationBuilder.CreateIndex(
                name: "IX_mensagens_id_destinatario",
                table: "mensagens",
                column: "id_destinatario");

            migrationBuilder.CreateIndex(
                name: "IX_mensagens_id_remetente",
                table: "mensagens",
                column: "id_remetente");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_stock_id_admin",
                table: "pedidos_stock",
                column: "id_admin");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_stock_id_agrupamento",
                table: "pedidos_stock",
                column: "id_agrupamento");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_stock_id_coordenador",
                table: "pedidos_stock",
                column: "id_coordenador");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_stock_id_escola",
                table: "pedidos_stock",
                column: "id_escola");

            migrationBuilder.CreateIndex(
                name: "IX_professores_id_bloco",
                table: "professores",
                column: "id_bloco");

            migrationBuilder.CreateIndex(
                name: "IX_reparos_id_equipamento",
                table: "reparos",
                column: "id_equipamento");

            migrationBuilder.CreateIndex(
                name: "IX_reparos_id_tecnico",
                table: "reparos",
                column: "id_tecnico");

            migrationBuilder.CreateIndex(
                name: "IX_salas_id_bloco",
                table: "salas",
                column: "id_bloco");

            migrationBuilder.CreateIndex(
                name: "IX_salas_id_professor_responsavel",
                table: "salas",
                column: "id_professor_responsavel");

            migrationBuilder.CreateIndex(
                name: "IX_status_equipamento_id_equipamento",
                table: "status_equipamento",
                column: "id_equipamento");

            migrationBuilder.CreateIndex(
                name: "IX_stock_empresa_id_admin",
                table: "stock_empresa",
                column: "id_admin");

            migrationBuilder.CreateIndex(
                name: "IX_stock_empresa_id_agrupamento",
                table: "stock_empresa",
                column: "id_agrupamento");

            migrationBuilder.CreateIndex(
                name: "IX_stock_empresa_id_empresa",
                table: "stock_empresa",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_stock_empresa_id_escola",
                table: "stock_empresa",
                column: "id_escola");

            migrationBuilder.CreateIndex(
                name: "IX_stock_empresa_id_tecnico",
                table: "stock_empresa",
                column: "id_tecnico");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_id_admin",
                table: "tickets",
                column: "id_admin");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_id_equipamento",
                table: "tickets",
                column: "id_equipamento");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_id_escola",
                table: "tickets",
                column: "id_escola");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_id_tecnico",
                table: "tickets",
                column: "id_tecnico");

            migrationBuilder.AddForeignKey(
                name: "FK_escolas_agrupamentos_id_agrupamento",
                table: "escolas",
                column: "id_agrupamento",
                principalTable: "agrupamentos",
                principalColumn: "id_agrupamento");

            migrationBuilder.AddForeignKey(
                name: "FK_utilizadores_empresas_id_empresa",
                table: "utilizadores",
                column: "id_empresa",
                principalTable: "empresas",
                principalColumn: "id_empresa");
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
