using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservationSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentOverlapConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hard guarantee against double-booking: no two non-cancelled,
            // non-deleted appointments for the same service may have overlapping
            // [StartTime, EndTime) ranges. Enforced by a GiST exclusion constraint,
            // which protects against race conditions the application-level check
            // alone cannot. Requires btree_gist for the equality part on ServiceId.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql(
                """
                ALTER TABLE appointments
                ADD CONSTRAINT ck_appointments_no_overlap
                EXCLUDE USING gist (
                    "ServiceId" WITH =,
                    tstzrange("StartTime", "EndTime") WITH &&
                )
                WHERE ("Status" <> 'Cancelled' AND "IsDeleted" = false);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE appointments DROP CONSTRAINT IF EXISTS ck_appointments_no_overlap;");
        }
    }
}
