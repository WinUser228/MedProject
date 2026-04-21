using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalStatistics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BedTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BedTypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BedTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Beds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    BedTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MonthlyPlan = table.Column<int>(type: "int", nullable: false),
                    NotUsedSince = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beds_BedTypes_BedTypeId",
                        column: x => x.BedTypeId,
                        principalTable: "BedTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Beds_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ListPatientMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BedId = table.Column<int>(type: "int", nullable: false),
                    PatientsAtStartOfDay = table.Column<int>(type: "int", nullable: true),
                    TotalAdmitted = table.Column<int>(type: "int", nullable: true),
                    FromVillage = table.Column<int>(type: "int", nullable: true),
                    ChildrenUnder14 = table.Column<int>(type: "int", nullable: true),
                    TransferredFromOtherDepartments = table.Column<int>(type: "int", nullable: true),
                    TransferredToOtherDepartments = table.Column<int>(type: "int", nullable: true),
                    TotalDischarged = table.Column<int>(type: "int", nullable: true),
                    DischargedByTransfer = table.Column<int>(type: "int", nullable: true),
                    Died = table.Column<int>(type: "int", nullable: true),
                    TotalPatients = table.Column<int>(type: "int", nullable: true),
                    FromVillagePatient = table.Column<int>(type: "int", nullable: true),
                    MothersWithPatients = table.Column<int>(type: "int", nullable: true),
                    MothersDischarged = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListPatientMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListPatientMovements_Beds_BedId",
                        column: x => x.BedId,
                        principalTable: "Beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beds_BedTypeId",
                table: "Beds",
                column: "BedTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Beds_DepartmentId",
                table: "Beds",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ListPatientMovements_BedId",
                table: "ListPatientMovements",
                column: "BedId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListPatientMovements");

            migrationBuilder.DropTable(
                name: "Beds");

            migrationBuilder.DropTable(
                name: "BedTypes");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
