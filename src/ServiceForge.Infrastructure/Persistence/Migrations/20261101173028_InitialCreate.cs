using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ServiceForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dealership",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealership", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceType",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceType", x => x.Id);
                    table.CheckConstraint("CK_ServiceType_DurationMinutes_Positive", "[DurationMinutes] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Vehicle",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle", x => x.Id);
                    table.UniqueConstraint("AK_Vehicle_Id_CustomerId", x => new { x.Id, x.CustomerId });
                    table.ForeignKey(
                        name: "FK_Vehicle_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceBay",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DealershipId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBay", x => x.Id);
                    table.UniqueConstraint("AK_ServiceBay_Id_DealershipId", x => new { x.Id, x.DealershipId });
                    table.ForeignKey(
                        name: "FK_ServiceBay_Dealership_DealershipId",
                        column: x => x.DealershipId,
                        principalTable: "Dealership",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Technician",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DealershipId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technician", x => x.Id);
                    table.UniqueConstraint("AK_Technician_Id_DealershipId", x => new { x.Id, x.DealershipId });
                    table.ForeignKey(
                        name: "FK_Technician_Dealership_DealershipId",
                        column: x => x.DealershipId,
                        principalTable: "Dealership",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BayServiceCompatibility",
                columns: table => new
                {
                    ServiceBayId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceTypeId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BayServiceCompatibility", x => new { x.ServiceBayId, x.ServiceTypeId });
                    table.ForeignKey(
                        name: "FK_BayServiceCompatibility_ServiceBay_ServiceBayId",
                        column: x => x.ServiceBayId,
                        principalTable: "ServiceBay",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BayServiceCompatibility_ServiceType_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Appointment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    VehicleId = table.Column<long>(type: "bigint", nullable: false),
                    DealershipId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceTypeId = table.Column<long>(type: "bigint", nullable: false),
                    TechnicianId = table.Column<long>(type: "bigint", nullable: true),
                    ServiceBayId = table.Column<long>(type: "bigint", nullable: true),
                    ScheduledStartUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ScheduledEndUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    AssignmentStatus = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    ActualCompletedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointment", x => x.Id);
                    table.CheckConstraint("CK_Appointment_AssignedResources", "[AssignmentStatus] <> 'Assigned' OR ([TechnicianId] IS NOT NULL AND [ServiceBayId] IS NOT NULL)");
                    table.CheckConstraint("CK_Appointment_AssignmentStatus_Allowed", "[AssignmentStatus] IN ('Assigned', 'NeedsReassignment')");
                    table.CheckConstraint("CK_Appointment_CompletionConsistency", "([Status] = 'Completed' AND [ActualCompletedAtUtc] IS NOT NULL) OR ([Status] <> 'Completed' AND [ActualCompletedAtUtc] IS NULL)");
                    table.CheckConstraint("CK_Appointment_DurationMinutes_Positive", "[DurationMinutes] > 0");
                    table.CheckConstraint("CK_Appointment_EndAfterStart", "[ScheduledEndUtc] > [ScheduledStartUtc]");
                    table.CheckConstraint("CK_Appointment_NeedsReassignmentResource", "[AssignmentStatus] <> 'NeedsReassignment' OR [TechnicianId] IS NULL OR [ServiceBayId] IS NULL");
                    table.CheckConstraint("CK_Appointment_Start_WholeMinute", "DATEPART(second, [ScheduledStartUtc]) = 0 AND DATEPART(nanosecond, [ScheduledStartUtc]) = 0");
                    table.CheckConstraint("CK_Appointment_Status_Allowed", "[Status] IN ('Confirmed', 'Cancelled', 'Completed')");
                    table.ForeignKey(
                        name: "FK_Appointment_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appointment_Dealership_DealershipId",
                        column: x => x.DealershipId,
                        principalTable: "Dealership",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appointment_ServiceBay_ServiceBayId_DealershipId",
                        columns: x => new { x.ServiceBayId, x.DealershipId },
                        principalTable: "ServiceBay",
                        principalColumns: new[] { "Id", "DealershipId" });
                    table.ForeignKey(
                        name: "FK_Appointment_ServiceType_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appointment_Technician_TechnicianId_DealershipId",
                        columns: x => new { x.TechnicianId, x.DealershipId },
                        principalTable: "Technician",
                        principalColumns: new[] { "Id", "DealershipId" });
                    table.ForeignKey(
                        name: "FK_Appointment_Vehicle_VehicleId_CustomerId",
                        columns: x => new { x.VehicleId, x.CustomerId },
                        principalTable: "Vehicle",
                        principalColumns: new[] { "Id", "CustomerId" });
                });

            migrationBuilder.CreateTable(
                name: "TechnicianQualification",
                columns: table => new
                {
                    TechnicianId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceTypeId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianQualification", x => new { x.TechnicianId, x.ServiceTypeId });
                    table.ForeignKey(
                        name: "FK_TechnicianQualification_ServiceType_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TechnicianQualification_Technician_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technician",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecord",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false),
                    ResponseStatusCode = table.Column<short>(type: "smallint", nullable: false),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecord", x => x.Key);
                    table.CheckConstraint("CK_IdempotencyRecord_ExpiryAfterCreation", "[ExpiresAtUtc] > [CreatedAtUtc]");
                    table.ForeignKey(
                        name: "FK_IdempotencyRecord_Appointment_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointment",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Customer",
                columns: new[] { "Id", "CreatedAtUtc", "Name" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo Customer One" },
                    { 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo Customer Two" }
                });

            migrationBuilder.InsertData(
                table: "Dealership",
                columns: new[] { "Id", "CreatedAtUtc", "Name", "TimeZoneId" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bangkok ServiceForge", "Asia/Bangkok" },
                    { 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "London ServiceForge", "Europe/London" },
                    { 3L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "New York ServiceForge", "America/New_York" }
                });

            migrationBuilder.InsertData(
                table: "ServiceType",
                columns: new[] { "Id", "CreatedAtUtc", "DurationMinutes", "Name" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, "Oil Service" },
                    { 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 90, "Brake Inspection" },
                    { 3L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 120, "Annual Service" }
                });

            migrationBuilder.InsertData(
                table: "ServiceBay",
                columns: new[] { "Id", "CreatedAtUtc", "DealershipId", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1L, true, "Bangkok Bay One" },
                    { 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1L, true, "Bangkok Bay Two" },
                    { 3L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2L, true, "London Bay One" },
                    { 4L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3L, true, "New York Bay One" }
                });

            migrationBuilder.InsertData(
                table: "Technician",
                columns: new[] { "Id", "CreatedAtUtc", "DealershipId", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1L, true, "Bangkok Technician One" },
                    { 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1L, true, "Bangkok Technician Two" },
                    { 3L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2L, true, "London Technician One" },
                    { 4L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3L, true, "New York Technician One" }
                });

            migrationBuilder.InsertData(
                table: "Vehicle",
                columns: new[] { "Id", "CreatedAtUtc", "CustomerId", "RegistrationNumber" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1L, "SF-1001" },
                    { 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2L, "SF-2001" }
                });

            migrationBuilder.InsertData(
                table: "BayServiceCompatibility",
                columns: new[] { "ServiceBayId", "ServiceTypeId" },
                values: new object[,]
                {
                    { 1L, 1L },
                    { 1L, 2L },
                    { 2L, 1L },
                    { 3L, 2L },
                    { 4L, 3L }
                });

            migrationBuilder.InsertData(
                table: "TechnicianQualification",
                columns: new[] { "ServiceTypeId", "TechnicianId" },
                values: new object[,]
                {
                    { 1L, 1L },
                    { 2L, 1L },
                    { 1L, 2L },
                    { 2L, 3L },
                    { 3L, 4L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_CustomerId_ScheduledStartUtc",
                table: "Appointment",
                columns: new[] { "CustomerId", "ScheduledStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_DealershipId_ScheduledStartUtc",
                table: "Appointment",
                columns: new[] { "DealershipId", "ScheduledStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_ServiceBayId_DealershipId",
                table: "Appointment",
                columns: new[] { "ServiceBayId", "DealershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_ServiceBayId_ScheduledStartUtc",
                table: "Appointment",
                columns: new[] { "ServiceBayId", "ScheduledStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_ServiceTypeId",
                table: "Appointment",
                column: "ServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_TechnicianId_DealershipId",
                table: "Appointment",
                columns: new[] { "TechnicianId", "DealershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_TechnicianId_ScheduledStartUtc",
                table: "Appointment",
                columns: new[] { "TechnicianId", "ScheduledStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_VehicleId_CustomerId",
                table: "Appointment",
                columns: new[] { "VehicleId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_VehicleId_ScheduledStartUtc",
                table: "Appointment",
                columns: new[] { "VehicleId", "ScheduledStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BayServiceCompatibility_ServiceTypeId_ServiceBayId",
                table: "BayServiceCompatibility",
                columns: new[] { "ServiceTypeId", "ServiceBayId" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecord_AppointmentId",
                table: "IdempotencyRecord",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecord_ExpiresAtUtc",
                table: "IdempotencyRecord",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBay_DealershipId_IsActive_Id",
                table: "ServiceBay",
                columns: new[] { "DealershipId", "IsActive", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Technician_DealershipId_IsActive_Id",
                table: "Technician",
                columns: new[] { "DealershipId", "IsActive", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianQualification_ServiceTypeId_TechnicianId",
                table: "TechnicianQualification",
                columns: new[] { "ServiceTypeId", "TechnicianId" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_CustomerId_Id",
                table: "Vehicle",
                columns: new[] { "CustomerId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BayServiceCompatibility");

            migrationBuilder.DropTable(
                name: "IdempotencyRecord");

            migrationBuilder.DropTable(
                name: "TechnicianQualification");

            migrationBuilder.DropTable(
                name: "Appointment");

            migrationBuilder.DropTable(
                name: "ServiceBay");

            migrationBuilder.DropTable(
                name: "ServiceType");

            migrationBuilder.DropTable(
                name: "Technician");

            migrationBuilder.DropTable(
                name: "Vehicle");

            migrationBuilder.DropTable(
                name: "Dealership");

            migrationBuilder.DropTable(
                name: "Customer");
        }
    }
}
