using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadedFileHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "UploadedFiles",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "UploadedFiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileNameNormalized",
                table: "UploadedFiles",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_ContentHash",
                table: "UploadedFiles",
                column: "ContentHash",
                unique: true,
                filter: "[ContentHash] IS NOT NULL AND [ContentHash] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UploadedFiles_ContentHash",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "OriginalFileNameNormalized",
                table: "UploadedFiles");
        }
    }
}
