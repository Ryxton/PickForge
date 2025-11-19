using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PickForge.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Predictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Week = table.Column<int>(type: "integer", nullable: false),
                    SeasonYear = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HomeTeam = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AwayTeam = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PredictedWinner = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    WasCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predictions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_CreatedUtc",
                table: "Predictions",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_GameId",
                table: "Predictions",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_Week_SeasonYear",
                table: "Predictions",
                columns: new[] { "Week", "SeasonYear" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Predictions");
        }
    }
}
