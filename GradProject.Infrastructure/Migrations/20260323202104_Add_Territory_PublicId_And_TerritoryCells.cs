using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Territory_PublicId_And_TerritoryCells : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "Territories",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.CreateTable(
                name: "TerritoryCells",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TerritoryId = table.Column<int>(type: "integer", nullable: false),
                    H3Index = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerritoryCells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerritoryCells_Territories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "Territories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Territories_PublicId",
                table: "Territories",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TerritoryCells_TerritoryId",
                table: "TerritoryCells",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TerritoryCells_TerritoryId_H3Index",
                table: "TerritoryCells",
                columns: new[] { "TerritoryId", "H3Index" },
                unique: true);

            // Hydrate normalized cells from legacy JSON array (best-effort).
            migrationBuilder.Sql(
                """
                INSERT INTO "TerritoryCells" ("TerritoryId", "H3Index")
                SELECT t."Id", elem
                FROM "Territories" AS t
                CROSS JOIN LATERAL jsonb_array_elements_text(
                    CASE
                        WHEN t."GeometryCells" IS NULL THEN '[]'::jsonb
                        WHEN jsonb_typeof(t."GeometryCells") = 'array' THEN t."GeometryCells"
                        ELSE '[]'::jsonb
                    END
                ) AS elem
                WHERE length(elem) > 0 AND length(elem) <= 64
                ON CONFLICT ("TerritoryId", "H3Index") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TerritoryCells");

            migrationBuilder.DropIndex(
                name: "IX_Territories_PublicId",
                table: "Territories");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Territories");
        }
    }
}
