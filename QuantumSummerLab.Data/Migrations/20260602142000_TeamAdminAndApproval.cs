using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QuantumSummerLab.Data;

#nullable disable

namespace QuantumSummerLab.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(QuantumSummerLabDbContext))]
    [Migration("20260602142000_TeamAdminAndApproval")]
    public partial class TeamAdminAndApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "TEAMS",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "TEAMS",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE [TEAMS]
                SET [IsApproved] = 1;

                IF NOT EXISTS (SELECT 1 FROM [TEAMS] WHERE [IsAdmin] = 1)
                BEGIN
                    WITH [FirstTeam] AS
                    (
                        SELECT TOP (1) [Id]
                        FROM [TEAMS]
                        ORDER BY [SysId]
                    )
                    UPDATE [t]
                    SET [t].[IsAdmin] = 1,
                        [t].[IsApproved] = 1
                    FROM [TEAMS] AS [t]
                    INNER JOIN [FirstTeam] AS [ft] ON [t].[Id] = [ft].[Id];
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "TEAMS");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "TEAMS");
        }
    }
}
