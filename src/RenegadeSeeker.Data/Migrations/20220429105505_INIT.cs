using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RenegadeSeeker.Data.Migrations
{
    public partial class INIT : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChainId = table.Column<long>(type: "bigint", nullable: false),
                    Address = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Decimals = table.Column<short>(type: "smallint", nullable: false),
                    TransactionHash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    ChainId = table.Column<long>(type: "bigint", nullable: false),
                    LogIndex = table.Column<long>(type: "bigint", nullable: false),
                    TransactionHash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    Height = table.Column<long>(type: "bigint", nullable: false),
                    From = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    To = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    Amount = table.Column<byte[]>(type: "bytea", nullable: false),
                    TokenId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfers", x => new { x.ChainId, x.TransactionHash, x.LogIndex });
                    table.ForeignKey(
                        name: "FK_Transfers_Tokens_TokenId",
                        column: x => x.TokenId,
                        principalTable: "Tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_TokenId",
                table: "Transfers",
                column: "TokenId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transfers");

            migrationBuilder.DropTable(
                name: "Tokens");
        }
    }
}
