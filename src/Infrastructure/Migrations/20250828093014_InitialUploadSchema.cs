using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialUploadSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UploadedFiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ByteSize = table.Column<long>(type: "bigint", nullable: true),
                    HashSha256 = table.Column<byte[]>(type: "varbinary(32)", nullable: true),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UploadedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UploadedSheets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadedFileId = table.Column<long>(type: "bigint", nullable: false),
                    SheetName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: true),
                    ParseStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedSheets_UploadedFiles_UploadedFileId",
                        column: x => x.UploadedFileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UploadedRows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadedSheetId = table.Column<long>(type: "bigint", nullable: false),
                    RowIndex = table.Column<int>(type: "int", nullable: false),
                    JsonPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NormalizedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sku = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Material = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Diameter = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedRows_UploadedSheets_UploadedSheetId",
                        column: x => x.UploadedSheetId,
                        principalTable: "UploadedSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductDescriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sku = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Material = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Diameter = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AttributesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SearchText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceFileId = table.Column<long>(type: "bigint", nullable: true),
                    SourceRowId = table.Column<long>(type: "bigint", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductDescriptions_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductDescriptions_UploadedFiles_SourceFileId",
                        column: x => x.SourceFileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductDescriptions_UploadedRows_SourceRowId",
                        column: x => x.SourceRowId,
                        principalTable: "UploadedRows",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UploadedRowMatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadedRowId = table.Column<long>(type: "bigint", nullable: false),
                    ProductDescriptionId = table.Column<long>(type: "bigint", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    MatchDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedRowMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedRowMatches_ProductDescriptions_ProductDescriptionId",
                        column: x => x.ProductDescriptionId,
                        principalTable: "ProductDescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UploadedRowMatches_UploadedRows_UploadedRowId",
                        column: x => x.UploadedRowId,
                        principalTable: "UploadedRows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductDescriptions_Sku",
                table: "ProductDescriptions",
                column: "Sku");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDescriptions_SourceFileId",
                table: "ProductDescriptions",
                column: "SourceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDescriptions_SourceRowId",
                table: "ProductDescriptions",
                column: "SourceRowId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDescriptions_SupplierId",
                table: "ProductDescriptions",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_HashSha256",
                table: "UploadedFiles",
                column: "HashSha256",
                unique: true,
                filter: "[HashSha256] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedRowMatches_ProductDescriptionId",
                table: "UploadedRowMatches",
                column: "ProductDescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedRowMatches_UploadedRowId",
                table: "UploadedRowMatches",
                column: "UploadedRowId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedRowMatches_UploadedRowId_ProductDescriptionId",
                table: "UploadedRowMatches",
                columns: new[] { "UploadedRowId", "ProductDescriptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedRows_Sku",
                table: "UploadedRows",
                column: "Sku");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedRows_UploadedSheetId_RowIndex",
                table: "UploadedRows",
                columns: new[] { "UploadedSheetId", "RowIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedSheets_UploadedFileId",
                table: "UploadedSheets",
                column: "UploadedFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadedRowMatches");

            migrationBuilder.DropTable(
                name: "ProductDescriptions");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "UploadedRows");

            migrationBuilder.DropTable(
                name: "UploadedSheets");

            migrationBuilder.DropTable(
                name: "UploadedFiles");
        }
    }
}
