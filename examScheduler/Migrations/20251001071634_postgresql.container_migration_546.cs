using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace examScheduler.Migrations
{
	/// <inheritdoc />
	public partial class postgresqlcontainer_migration_546 : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "AuditLogs",
				columns: table => new
				{
					Id = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					TimestampUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					Action = table.Column<string>(type: "text", nullable: false),
					PerformedBy = table.Column<string>(type: "text", nullable: false),
					Details = table.Column<string>(type: "text", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_AuditLogs", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Classrooms",
				columns: table => new
				{
					Id = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					Name = table.Column<string>(type: "text", nullable: false),
					CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Classrooms", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Subjects",
				columns: table => new
				{
					Id = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					Name = table.Column<string>(type: "text", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Subjects", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Students",
				columns: table => new
				{
					Id = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					RegisterUsername = table.Column<string>(type: "text", nullable: false),
					RegisterUri = table.Column<string>(type: "text", nullable: false),
					Name = table.Column<string>(type: "text", nullable: false),
					Surname = table.Column<string>(type: "text", nullable: false),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					Salt = table.Column<string>(type: "text", nullable: false),
					Hash = table.Column<string>(type: "text", nullable: false),
					Permissions = table.Column<int>(type: "integer", nullable: false),
					ClassroomId = table.Column<int>(type: "integer", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Students", x => x.Id);
					table.ForeignKey(
						name: "FK_Students_Classrooms_ClassroomId",
						column: x => x.ClassroomId,
						principalTable: "Classrooms",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Timetables",
				columns: table => new
				{
					Id = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					ClassroomId = table.Column<int>(type: "integer", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Timetables", x => x.Id);
					table.ForeignKey(
						name: "FK_Timetables_Classrooms_ClassroomId",
						column: x => x.ClassroomId,
						principalTable: "Classrooms",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Schedules",
				columns: table => new
				{
					Id = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					AutoLockIn = table.Column<int>(type: "integer", nullable: false),
					lockInDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
					ClassroomId = table.Column<int>(type: "integer", nullable: false),
					SubjectId = table.Column<int>(type: "integer", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Schedules", x => x.Id);
					table.ForeignKey(
						name: "FK_Schedules_Classrooms_ClassroomId",
						column: x => x.ClassroomId,
						principalTable: "Classrooms",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_Schedules_Subjects_SubjectId",
						column: x => x.SubjectId,
						principalTable: "Subjects",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Week",
				columns: table => new
				{
					Id = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					Start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					TimetableId = table.Column<int>(type: "integer", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Week", x => x.Id);
					table.ForeignKey(
						name: "FK_Week_Timetables_TimetableId",
						column: x => x.TimetableId,
						principalTable: "Timetables",
						principalColumn: "Id");
				});

			migrationBuilder.CreateTable(
				name: "Teachers",
				columns: table => new
				{
					Id = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					Name = table.Column<string>(type: "text", nullable: false),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					TimetableId = table.Column<int>(type: "integer", nullable: true),
					ScheduleId = table.Column<int>(type: "integer", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Teachers", x => x.Id);
					table.ForeignKey(
						name: "FK_Teachers_Schedules_ScheduleId",
						column: x => x.ScheduleId,
						principalTable: "Schedules",
						principalColumn: "Id");
					table.ForeignKey(
						name: "FK_Teachers_Timetables_TimetableId",
						column: x => x.TimetableId,
						principalTable: "Timetables",
						principalColumn: "Id");
				});

			migrationBuilder.CreateTable(
				name: "Day",
				columns: table => new
				{
					Id = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					DayOfWeek = table.Column<int>(type: "integer", nullable: false),
					WeekId = table.Column<int>(type: "integer", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Day", x => x.Id);
					table.ForeignKey(
						name: "FK_Day_Week_WeekId",
						column: x => x.WeekId,
						principalTable: "Week",
						principalColumn: "Id");
				});

			migrationBuilder.CreateTable(
				name: "ClassroomTeacher",
				columns: table => new
				{
					ClassroomsId = table.Column<int>(type: "integer", nullable: false),
					TeachersId = table.Column<int>(type: "integer", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ClassroomTeacher", x => new { x.ClassroomsId, x.TeachersId });
					table.ForeignKey(
						name: "FK_ClassroomTeacher_Classrooms_ClassroomsId",
						column: x => x.ClassroomsId,
						principalTable: "Classrooms",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_ClassroomTeacher_Teachers_TeachersId",
						column: x => x.TeachersId,
						principalTable: "Teachers",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "SubjectTeacher",
				columns: table => new
				{
					SubjectsId = table.Column<int>(type: "integer", nullable: false),
					TeachersId = table.Column<int>(type: "integer", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_SubjectTeacher", x => new { x.SubjectsId, x.TeachersId });
					table.ForeignKey(
						name: "FK_SubjectTeacher_Subjects_SubjectsId",
						column: x => x.SubjectsId,
						principalTable: "Subjects",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_SubjectTeacher_Teachers_TeachersId",
						column: x => x.TeachersId,
						principalTable: "Teachers",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Lesson",
				columns: table => new
				{
					Id = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					Start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					StartHour = table.Column<byte>(type: "smallint", nullable: false),
					DurationInHours = table.Column<byte>(type: "smallint", nullable: false),
					SubjectId = table.Column<int>(type: "integer", nullable: false),
					DayId = table.Column<int>(type: "integer", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Lesson", x => x.Id);
					table.ForeignKey(
						name: "FK_Lesson_Day_DayId",
						column: x => x.DayId,
						principalTable: "Day",
						principalColumn: "Id");
					table.ForeignKey(
						name: "FK_Lesson_Subjects_SubjectId",
						column: x => x.SubjectId,
						principalTable: "Subjects",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "ExamSlot",
				columns: table => new
				{
					Id = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					PeriodId = table.Column<int>(type: "integer", nullable: false),
					ClassroomId = table.Column<int>(type: "integer", nullable: false),
					ScheduleId = table.Column<int>(type: "integer", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ExamSlot", x => x.Id);
					table.ForeignKey(
						name: "FK_ExamSlot_Classrooms_ClassroomId",
						column: x => x.ClassroomId,
						principalTable: "Classrooms",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_ExamSlot_Lesson_PeriodId",
						column: x => x.PeriodId,
						principalTable: "Lesson",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_ExamSlot_Schedules_ScheduleId",
						column: x => x.ScheduleId,
						principalTable: "Schedules",
						principalColumn: "Id");
				});

			migrationBuilder.CreateIndex(
				name: "IX_ClassroomTeacher_TeachersId",
				table: "ClassroomTeacher",
				column: "TeachersId");

			migrationBuilder.CreateIndex(
				name: "IX_Day_WeekId",
				table: "Day",
				column: "WeekId");

			migrationBuilder.CreateIndex(
				name: "IX_ExamSlot_ClassroomId",
				table: "ExamSlot",
				column: "ClassroomId");

			migrationBuilder.CreateIndex(
				name: "IX_ExamSlot_PeriodId",
				table: "ExamSlot",
				column: "PeriodId");

			migrationBuilder.CreateIndex(
				name: "IX_ExamSlot_ScheduleId",
				table: "ExamSlot",
				column: "ScheduleId");

			migrationBuilder.CreateIndex(
				name: "IX_Lesson_DayId",
				table: "Lesson",
				column: "DayId");

			migrationBuilder.CreateIndex(
				name: "IX_Lesson_SubjectId",
				table: "Lesson",
				column: "SubjectId");

			migrationBuilder.CreateIndex(
				name: "IX_Schedules_ClassroomId",
				table: "Schedules",
				column: "ClassroomId");

			migrationBuilder.CreateIndex(
				name: "IX_Schedules_SubjectId",
				table: "Schedules",
				column: "SubjectId");

			migrationBuilder.CreateIndex(
				name: "IX_Students_ClassroomId",
				table: "Students",
				column: "ClassroomId");

			migrationBuilder.CreateIndex(
				name: "IX_SubjectTeacher_TeachersId",
				table: "SubjectTeacher",
				column: "TeachersId");

			migrationBuilder.CreateIndex(
				name: "IX_Teachers_ScheduleId",
				table: "Teachers",
				column: "ScheduleId");

			migrationBuilder.CreateIndex(
				name: "IX_Teachers_TimetableId",
				table: "Teachers",
				column: "TimetableId");

			migrationBuilder.CreateIndex(
				name: "IX_Timetables_ClassroomId",
				table: "Timetables",
				column: "ClassroomId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Week_TimetableId",
				table: "Week",
				column: "TimetableId");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "AuditLogs");

			migrationBuilder.DropTable(
				name: "ClassroomTeacher");

			migrationBuilder.DropTable(
				name: "ExamSlot");

			migrationBuilder.DropTable(
				name: "Students");

			migrationBuilder.DropTable(
				name: "SubjectTeacher");

			migrationBuilder.DropTable(
				name: "Lesson");

			migrationBuilder.DropTable(
				name: "Teachers");

			migrationBuilder.DropTable(
				name: "Day");

			migrationBuilder.DropTable(
				name: "Schedules");

			migrationBuilder.DropTable(
				name: "Week");

			migrationBuilder.DropTable(
				name: "Subjects");

			migrationBuilder.DropTable(
				name: "Timetables");

			migrationBuilder.DropTable(
				name: "Classrooms");
		}
	}
}
