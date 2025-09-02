using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finance.Tracking.Migrations
{
    /// <inheritdoc />
    public partial class TotalMarketValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TotalMarketValues",
                columns: table => new
                {
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    AsOf = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    MarketValue = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TotalMarketValues", x => new { x.AsOf, x.Type });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TotalMarketValues");
        }
    }
}
