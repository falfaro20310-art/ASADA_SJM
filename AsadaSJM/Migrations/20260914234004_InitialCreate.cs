using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AsadaSJM.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Abonados",
                columns: table => new
                {
                    IdAbonado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NumeroMedidor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abonados", x => x.IdAbonado);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    IdRol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreRol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.IdRol);
                });

            migrationBuilder.CreateTable(
                name: "Recibos",
                columns: table => new
                {
                    IdRecibo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAbonado = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recibos", x => x.IdRecibo);
                    table.ForeignKey(
                        name: "FK_Recibos_Abonados_IdAbonado",
                        column: x => x.IdAbonado,
                        principalTable: "Abonados",
                        principalColumn: "IdAbonado",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContrasenaHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    IdRol = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                    table.ForeignKey(
                        name: "FK_Usuarios_Roles_IdRol",
                        column: x => x.IdRol,
                        principalTable: "Roles",
                        principalColumn: "IdRol",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Averias",
                columns: table => new
                {
                    IdAveria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAbonado = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaReporte = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Averias", x => x.IdAveria);
                    table.ForeignKey(
                        name: "FK_Averias_Abonados_IdAbonado",
                        column: x => x.IdAbonado,
                        principalTable: "Abonados",
                        principalColumn: "IdAbonado",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Averias_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Bitacoras",
                columns: table => new
                {
                    IdBitacora = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bitacoras", x => x.IdBitacora);
                    table.ForeignKey(
                        name: "FK_Bitacoras_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tramites",
                columns: table => new
                {
                    IdTramite = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAbonado = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    TipoTramite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tramites", x => x.IdTramite);
                    table.ForeignKey(
                        name: "FK_Tramites_Abonados_IdAbonado",
                        column: x => x.IdAbonado,
                        principalTable: "Abonados",
                        principalColumn: "IdAbonado",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tramites_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    IdDocumento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdTramite = table.Column<int>(type: "int", nullable: true),
                    NombreArchivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.IdDocumento);
                    table.ForeignKey(
                        name: "FK_Documentos_Tramites_IdTramite",
                        column: x => x.IdTramite,
                        principalTable: "Tramites",
                        principalColumn: "IdTramite");
                });

            migrationBuilder.InsertData(
                table: "Abonados",
                columns: new[] { "IdAbonado", "Activo", "Direccion", "Nombre", "NumeroMedidor" },
                values: new object[,]
                {
                    { 1, true, "Calle Los Robles", "María Vargas", "MED-0231" },
                    { 2, true, "Barrio Central", "Juan Rojas", "MED-0198" },
                    { 3, false, "Calle La Montaña", "Lucía Solano", "MED-0177" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "IdRol", "NombreRol" },
                values: new object[,]
                {
                    { 1, "Administrador" },
                    { 2, "Operativo" },
                    { 3, "Abonado" }
                });

            migrationBuilder.InsertData(
                table: "Averias",
                columns: new[] { "IdAveria", "Descripcion", "Estado", "FechaReporte", "IdAbonado", "IdUsuario" },
                values: new object[] { 1, "Fuga en tubería principal", "Pendiente", new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), 1, null });

            migrationBuilder.InsertData(
                table: "Recibos",
                columns: new[] { "IdRecibo", "FechaEmision", "IdAbonado", "Monto" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 6450m },
                    { 2, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 5120m }
                });

            migrationBuilder.InsertData(
                table: "Tramites",
                columns: new[] { "IdTramite", "Estado", "FechaSolicitud", "IdAbonado", "IdUsuario", "TipoTramite" },
                values: new object[,]
                {
                    { 1, "Pendiente", new DateTime(2026, 9, 1, 8, 30, 0, 0, DateTimeKind.Unspecified), 1, null, "Cambio de titular" },
                    { 2, "En revisión", new DateTime(2026, 9, 2, 10, 0, 0, 0, DateTimeKind.Unspecified), 2, null, "Solicitud de conexión" },
                    { 3, "Aprobado", new DateTime(2026, 9, 3, 11, 0, 0, 0, DateTimeKind.Unspecified), 3, null, "Reclamo de facturación" }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "IdUsuario", "Activo", "ContrasenaHash", "Correo", "IdRol", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "temporal", "pjimenez@asadasjm.cr", 1, "P. Jiménez" },
                    { 2, true, "temporal", "cmora@asadasjm.cr", 2, "C. Mora" }
                });

            migrationBuilder.InsertData(
                table: "Averias",
                columns: new[] { "IdAveria", "Descripcion", "Estado", "FechaReporte", "IdAbonado", "IdUsuario" },
                values: new object[,]
                {
                    { 2, "Baja presión de agua", "En proceso", new DateTime(2026, 9, 2, 9, 30, 0, 0, DateTimeKind.Unspecified), 2, 2 },
                    { 3, "Medidor dañado", "Resuelto", new DateTime(2026, 9, 3, 10, 0, 0, 0, DateTimeKind.Unspecified), 3, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Averias_IdAbonado",
                table: "Averias",
                column: "IdAbonado");

            migrationBuilder.CreateIndex(
                name: "IX_Averias_IdUsuario",
                table: "Averias",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Bitacoras_IdUsuario",
                table: "Bitacoras",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_IdTramite",
                table: "Documentos",
                column: "IdTramite");

            migrationBuilder.CreateIndex(
                name: "IX_Recibos_IdAbonado",
                table: "Recibos",
                column: "IdAbonado");

            migrationBuilder.CreateIndex(
                name: "IX_Tramites_IdAbonado",
                table: "Tramites",
                column: "IdAbonado");

            migrationBuilder.CreateIndex(
                name: "IX_Tramites_IdUsuario",
                table: "Tramites",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdRol",
                table: "Usuarios",
                column: "IdRol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Averias");

            migrationBuilder.DropTable(
                name: "Bitacoras");

            migrationBuilder.DropTable(
                name: "Documentos");

            migrationBuilder.DropTable(
                name: "Recibos");

            migrationBuilder.DropTable(
                name: "Tramites");

            migrationBuilder.DropTable(
                name: "Abonados");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
