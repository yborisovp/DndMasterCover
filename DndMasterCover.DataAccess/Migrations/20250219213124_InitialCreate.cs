using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DndMasterCover.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dnd");

            migrationBuilder.CreateTable(
                name: "enemies",
                schema: "dnd",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    danger_level = table.Column<float>(type: "real", nullable: false),
                    hp = table.Column<int>(type: "integer", nullable: false),
                    max_hp = table.Column<int>(type: "integer", nullable: true),
                    @class = table.Column<string>(name: "class", type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    abilities = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_enemies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "enemy_searches",
                schema: "dnd",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    danger = table.Column<float>(type: "real", nullable: false),
                    link = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_enemy_searches", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_enemy_searches_name",
                schema: "dnd",
                table: "enemy_searches",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "russian");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "enemies",
                schema: "dnd");

            migrationBuilder.DropTable(
                name: "enemy_searches",
                schema: "dnd");
        }
    }
}
