using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicineIdentification.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
             IF OBJECT_ID(N'[Medicines]', N'U') IS NULL
             BEGIN
                 CREATE TABLE [Medicines] (
                     [Id] INT NOT NULL IDENTITY(1,1),
                     [Label] NVARCHAR(MAX) NOT NULL,
                     [Manufacturer] NVARCHAR(MAX) NOT NULL,
                     [Composition] NVARCHAR(MAX) NOT NULL,
                     [Uses] NVARCHAR(MAX) NOT NULL,
                     [SideEffects] NVARCHAR(MAX) NOT NULL,
                     [Dosage] NVARCHAR(MAX) NOT NULL,
                     CONSTRAINT [PK_Medicines] PRIMARY KEY ([Id])
                 );
             END
         ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
             IF OBJECT_ID(N'[Medicines]', N'U') IS NOT NULL
             BEGIN
                 DROP TABLE [Medicines];
             END
         ");
        }
    }
}
