using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLearning.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CouponCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coupons_Courses_CourseId",
                table: "Coupons");

            migrationBuilder.AddForeignKey(
                name: "FK_Coupons_Courses_CourseId",
                table: "Coupons",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coupons_Courses_CourseId",
                table: "Coupons");

            migrationBuilder.AddForeignKey(
                name: "FK_Coupons_Courses_CourseId",
                table: "Coupons",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");
        }
    }
}
