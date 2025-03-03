using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkySync_FileManager_API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFileInfoIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing primary key
            migrationBuilder.DropPrimaryKey(
                name: "PK_FileInfos",
                table: "FileInfos");

            // Drop the existing Id column
            migrationBuilder.DropColumn(
                name: "Id",
                table: "FileInfos");

            // Add a new Id column of type Guid
            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "FileInfos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()"); // Automatically generate GUIDs for existing rows

            // Add the primary key constraint
            migrationBuilder.AddPrimaryKey(
                name: "PK_FileInfos",
                table: "FileInfos",
                column: "Id");
        }

        /// <inheritdoc />
       /* protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "FileInfos",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier")
                .Annotation("SqlServer:Identity", "1, 1");
        }*/
    }
}
