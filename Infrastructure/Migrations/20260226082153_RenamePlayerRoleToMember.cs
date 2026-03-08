using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenamePlayerRoleToMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"AspNetRoles\" SET \"Name\" = 'Member', \"NormalizedName\" = 'MEMBER' WHERE \"Name\" = 'Player';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"AspNetRoles\" SET \"Name\" = 'Player', \"NormalizedName\" = 'PLAYER' WHERE \"Name\" = 'Member';");
        }
    }
}
