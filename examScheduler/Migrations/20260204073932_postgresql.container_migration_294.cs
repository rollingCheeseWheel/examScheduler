using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace examScheduler.Migrations;

/// <inheritdoc />
public partial class postgresqlcontainer_migration_294 : Migration
{
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable(
			name: "AspNetRoles",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
				NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
				ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_AspNetRoles", x => x.Id);
			});

		migrationBuilder.CreateTable(
			name: "Calendar",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				LastsUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
				xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Calendar", x => x.Id);
			});

		migrationBuilder.CreateTable(
			name: "RefreshSessions",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				ExpirationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
				TokenValue = table.Column<string>(type: "text", nullable: false),
				UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
				xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_RefreshSessions", x => x.Id);
			});

		migrationBuilder.CreateTable(
			name: "Schools",
			columns: table => new
			{
				SchoolId = table.Column<string>(type: "text", nullable: false),
				Name = table.Column<string>(type: "text", nullable: false),
				RegisterUri = table.Column<string>(type: "text", nullable: false),
				ClientId = table.Column<string>(type: "text", nullable: false),
				Secret = table.Column<string>(type: "text", nullable: false),
				IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
				xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Schools", x => x.SchoolId);
			});

		migrationBuilder.CreateTable(
			name: "Subjects",
			columns: table => new
			{
				Name = table.Column<string>(type: "text", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Subjects", x => x.Name);
			});

		migrationBuilder.CreateTable(
			name: "AspNetRoleClaims",
			columns: table => new
			{
				Id = table.Column<int>(type: "integer", nullable: false)
					.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
				RoleId = table.Column<Guid>(type: "uuid", nullable: false),
				ClaimType = table.Column<string>(type: "text", nullable: true),
				ClaimValue = table.Column<string>(type: "text", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
				table.ForeignKey(
					name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
					column: x => x.RoleId,
					principalTable: "AspNetRoles",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "AspNetUsers",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				SchoolId = table.Column<string>(type: "text", nullable: false),
				RegiserId = table.Column<long>(type: "bigint", nullable: false),
				Role = table.Column<int>(type: "integer", nullable: false),
				FirstName = table.Column<string>(type: "text", nullable: false),
				LastName = table.Column<string>(type: "text", nullable: false),
				UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
				NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
				Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
				NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
				EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
				PasswordHash = table.Column<string>(type: "text", nullable: true),
				SecurityStamp = table.Column<string>(type: "text", nullable: true),
				ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
				PhoneNumber = table.Column<string>(type: "text", nullable: true),
				PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
				TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
				LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
				LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
				AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_AspNetUsers", x => x.Id);
				table.ForeignKey(
					name: "FK_AspNetUsers_Schools_SchoolId",
					column: x => x.SchoolId,
					principalTable: "Schools",
					principalColumn: "SchoolId",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "Lesson",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				FromHour = table.Column<int>(type: "integer", nullable: false),
				ToHour = table.Column<int>(type: "integer", nullable: false),
				Occurances = table.Column<DateTimeOffset[ ]>(type: "timestamp with time zone[]", nullable: false),
				Name = table.Column<string>(type: "text", nullable: false),
				SubjectName = table.Column<string>(type: "text", nullable: false),
				xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
				CalendarId = table.Column<Guid>(type: "uuid", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Lesson", x => x.Id);
				table.ForeignKey(
					name: "FK_Lesson_Calendar_CalendarId",
					column: x => x.CalendarId,
					principalTable: "Calendar",
					principalColumn: "Id");
				table.ForeignKey(
					name: "FK_Lesson_Subjects_SubjectName",
					column: x => x.SubjectName,
					principalTable: "Subjects",
					principalColumn: "Name",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "AspNetUserClaims",
			columns: table => new
			{
				Id = table.Column<int>(type: "integer", nullable: false)
					.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
				UserId = table.Column<Guid>(type: "uuid", nullable: false),
				ClaimType = table.Column<string>(type: "text", nullable: true),
				ClaimValue = table.Column<string>(type: "text", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
				table.ForeignKey(
					name: "FK_AspNetUserClaims_AspNetUsers_UserId",
					column: x => x.UserId,
					principalTable: "AspNetUsers",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "AspNetUserLogins",
			columns: table => new
			{
				LoginProvider = table.Column<string>(type: "text", nullable: false),
				ProviderKey = table.Column<string>(type: "text", nullable: false),
				ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
				UserId = table.Column<Guid>(type: "uuid", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
				table.ForeignKey(
					name: "FK_AspNetUserLogins_AspNetUsers_UserId",
					column: x => x.UserId,
					principalTable: "AspNetUsers",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "AspNetUserRoles",
			columns: table => new
			{
				UserId = table.Column<Guid>(type: "uuid", nullable: false),
				RoleId = table.Column<Guid>(type: "uuid", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
				table.ForeignKey(
					name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
					column: x => x.RoleId,
					principalTable: "AspNetRoles",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_AspNetUserRoles_AspNetUsers_UserId",
					column: x => x.UserId,
					principalTable: "AspNetUsers",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "AspNetUserTokens",
			columns: table => new
			{
				UserId = table.Column<Guid>(type: "uuid", nullable: false),
				LoginProvider = table.Column<string>(type: "text", nullable: false),
				Name = table.Column<string>(type: "text", nullable: false),
				Value = table.Column<string>(type: "text", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
				table.ForeignKey(
					name: "FK_AspNetUserTokens_AspNetUsers_UserId",
					column: x => x.UserId,
					principalTable: "AspNetUsers",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "AuditLog",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				Action = table.Column<string>(type: "text", nullable: false),
				OriginType = table.Column<int>(type: "integer", nullable: false),
				OriginId = table.Column<Guid>(type: "uuid", nullable: false),
				OriginName = table.Column<string>(type: "text", nullable: false),
				TargetType = table.Column<int>(type: "integer", nullable: true),
				TargetId = table.Column<Guid>(type: "uuid", nullable: true),
				TargetName = table.Column<string>(type: "text", nullable: true),
				Description = table.Column<string>(type: "text", nullable: true),
				xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
				ScheduleId = table.Column<Guid>(type: "uuid", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_AuditLog", x => x.Id);
			});

		migrationBuilder.CreateTable(
			name: "Classrooms",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				Name = table.Column<string>(type: "text", nullable: false),
				SchoolId = table.Column<string>(type: "text", nullable: false),
				RegisterId = table.Column<int[ ]>(type: "integer[]", nullable: false),
				CalendarId = table.Column<Guid>(type: "uuid", nullable: false),
				xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
				TeacherProfileId = table.Column<Guid>(type: "uuid", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Classrooms", x => x.Id);
				table.ForeignKey(
					name: "FK_Classrooms_Calendar_CalendarId",
					column: x => x.CalendarId,
					principalTable: "Calendar",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_Classrooms_Schools_SchoolId",
					column: x => x.SchoolId,
					principalTable: "Schools",
					principalColumn: "SchoolId",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "Schedule",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
				EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
				AutoLockIn = table.Column<int>(type: "integer", nullable: false),
				AutoLockInOffset = table.Column<TimeSpan>(type: "interval", nullable: false),
				Description = table.Column<string>(type: "text", nullable: true),
				SlotFillingBehaviour = table.Column<int>(type: "integer", nullable: false),
				SubjectName = table.Column<string>(type: "text", nullable: false),
				xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
				ClassroomId = table.Column<Guid>(type: "uuid", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Schedule", x => x.Id);
				table.ForeignKey(
					name: "FK_Schedule_Classrooms_ClassroomId",
					column: x => x.ClassroomId,
					principalTable: "Classrooms",
					principalColumn: "Id");
				table.ForeignKey(
					name: "FK_Schedule_Subjects_SubjectName",
					column: x => x.SubjectName,
					principalTable: "Subjects",
					principalColumn: "Name",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "StudentProfiles",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				ClassroomId = table.Column<Guid>(type: "uuid", nullable: false),
				xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_StudentProfiles", x => x.Id);
				table.ForeignKey(
					name: "FK_StudentProfiles_AspNetUsers_Id",
					column: x => x.Id,
					principalTable: "AspNetUsers",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_StudentProfiles_Classrooms_ClassroomId",
					column: x => x.ClassroomId,
					principalTable: "Classrooms",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "ExamSlot",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
				ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
				MinParticipants = table.Column<int>(type: "integer", nullable: false),
				MaxParticipants = table.Column<int>(type: "integer", nullable: false),
				LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
				ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
				xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_ExamSlot", x => x.Id);
				table.ForeignKey(
					name: "FK_ExamSlot_Schedule_ScheduleId",
					column: x => x.ScheduleId,
					principalTable: "Schedule",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "SwapRequest",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
				RequestingStudentName = table.Column<string>(type: "text", nullable: false),
				RequestingStudentId = table.Column<Guid>(type: "uuid", nullable: false),
				RequestedSlotId = table.Column<Guid>(type: "uuid", nullable: false),
				xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_SwapRequest", x => x.Id);
				table.ForeignKey(
					name: "FK_SwapRequest_Schedule_ScheduleId",
					column: x => x.ScheduleId,
					principalTable: "Schedule",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "Teachers",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				FirstName = table.Column<string>(type: "text", nullable: false),
				LastName = table.Column<string>(type: "text", nullable: false),
				SchoolId = table.Column<string>(type: "text", nullable: false),
				TeacherProfileId = table.Column<Guid>(type: "uuid", nullable: true),
				xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
				ScheduleId = table.Column<Guid>(type: "uuid", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Teachers", x => x.Id);
				table.ForeignKey(
					name: "FK_Teachers_Schedule_ScheduleId",
					column: x => x.ScheduleId,
					principalTable: "Schedule",
					principalColumn: "Id");
				table.ForeignKey(
					name: "FK_Teachers_Schools_SchoolId",
					column: x => x.SchoolId,
					principalTable: "Schools",
					principalColumn: "SchoolId",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "ExamSlotStudentProfile",
			columns: table => new
			{
				ExamSlotId = table.Column<Guid>(type: "uuid", nullable: false),
				ParticipantsId = table.Column<Guid>(type: "uuid", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_ExamSlotStudentProfile", x => new { x.ExamSlotId, x.ParticipantsId });
				table.ForeignKey(
					name: "FK_ExamSlotStudentProfile_ExamSlot_ExamSlotId",
					column: x => x.ExamSlotId,
					principalTable: "ExamSlot",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_ExamSlotStudentProfile_StudentProfiles_ParticipantsId",
					column: x => x.ParticipantsId,
					principalTable: "StudentProfiles",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "ClassroomTeacher",
			columns: table => new
			{
				ClassroomId = table.Column<Guid>(type: "uuid", nullable: false),
				TeachersId = table.Column<Guid>(type: "uuid", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_ClassroomTeacher", x => new { x.ClassroomId, x.TeachersId });
				table.ForeignKey(
					name: "FK_ClassroomTeacher_Classrooms_ClassroomId",
					column: x => x.ClassroomId,
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
			name: "LessonTeacher",
			columns: table => new
			{
				LessonId = table.Column<Guid>(type: "uuid", nullable: false),
				TeachersId = table.Column<Guid>(type: "uuid", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_LessonTeacher", x => new { x.LessonId, x.TeachersId });
				table.ForeignKey(
					name: "FK_LessonTeacher_Lesson_LessonId",
					column: x => x.LessonId,
					principalTable: "Lesson",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_LessonTeacher_Teachers_TeachersId",
					column: x => x.TeachersId,
					principalTable: "Teachers",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "SubjectTeacher",
			columns: table => new
			{
				SubjectsName = table.Column<string>(type: "text", nullable: false),
				TeacherId = table.Column<Guid>(type: "uuid", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_SubjectTeacher", x => new { x.SubjectsName, x.TeacherId });
				table.ForeignKey(
					name: "FK_SubjectTeacher_Subjects_SubjectsName",
					column: x => x.SubjectsName,
					principalTable: "Subjects",
					principalColumn: "Name",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_SubjectTeacher_Teachers_TeacherId",
					column: x => x.TeacherId,
					principalTable: "Teachers",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "TeacherProfiles",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				TeacherId = table.Column<Guid>(type: "uuid", nullable: true),
				xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_TeacherProfiles", x => x.Id);
				table.ForeignKey(
					name: "FK_TeacherProfiles_AspNetUsers_Id",
					column: x => x.Id,
					principalTable: "AspNetUsers",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_TeacherProfiles_Teachers_TeacherId",
					column: x => x.TeacherId,
					principalTable: "Teachers",
					principalColumn: "Id");
			});

		migrationBuilder.CreateIndex(
			name: "IX_AspNetRoleClaims_RoleId",
			table: "AspNetRoleClaims",
			column: "RoleId");

		migrationBuilder.CreateIndex(
			name: "RoleNameIndex",
			table: "AspNetRoles",
			column: "NormalizedName",
			unique: true);

		migrationBuilder.CreateIndex(
			name: "IX_AspNetUserClaims_UserId",
			table: "AspNetUserClaims",
			column: "UserId");

		migrationBuilder.CreateIndex(
			name: "IX_AspNetUserLogins_UserId",
			table: "AspNetUserLogins",
			column: "UserId");

		migrationBuilder.CreateIndex(
			name: "IX_AspNetUserRoles_RoleId",
			table: "AspNetUserRoles",
			column: "RoleId");

		migrationBuilder.CreateIndex(
			name: "EmailIndex",
			table: "AspNetUsers",
			column: "NormalizedEmail");

		migrationBuilder.CreateIndex(
			name: "IX_AspNetUsers_SchoolId",
			table: "AspNetUsers",
			column: "SchoolId");

		migrationBuilder.CreateIndex(
			name: "IX_AspNetUsers_UserName_SchoolId",
			table: "AspNetUsers",
			columns: new[ ] { "UserName", "SchoolId" },
			unique: true);

		migrationBuilder.CreateIndex(
			name: "UserNameIndex",
			table: "AspNetUsers",
			column: "NormalizedUserName",
			unique: true);

		migrationBuilder.CreateIndex(
			name: "IX_AuditLog_ScheduleId",
			table: "AuditLog",
			column: "ScheduleId");

		migrationBuilder.CreateIndex(
			name: "IX_Classrooms_CalendarId",
			table: "Classrooms",
			column: "CalendarId");

		migrationBuilder.CreateIndex(
			name: "IX_Classrooms_SchoolId_Name",
			table: "Classrooms",
			columns: new[ ] { "SchoolId", "Name" },
			unique: true);

		migrationBuilder.CreateIndex(
			name: "IX_Classrooms_TeacherProfileId",
			table: "Classrooms",
			column: "TeacherProfileId");

		migrationBuilder.CreateIndex(
			name: "IX_ClassroomTeacher_TeachersId",
			table: "ClassroomTeacher",
			column: "TeachersId");

		migrationBuilder.CreateIndex(
			name: "IX_ExamSlot_ScheduleId",
			table: "ExamSlot",
			column: "ScheduleId");

		migrationBuilder.CreateIndex(
			name: "IX_ExamSlotStudentProfile_ParticipantsId",
			table: "ExamSlotStudentProfile",
			column: "ParticipantsId");

		migrationBuilder.CreateIndex(
			name: "IX_Lesson_CalendarId",
			table: "Lesson",
			column: "CalendarId");

		migrationBuilder.CreateIndex(
			name: "IX_Lesson_SubjectName",
			table: "Lesson",
			column: "SubjectName");

		migrationBuilder.CreateIndex(
			name: "IX_LessonTeacher_TeachersId",
			table: "LessonTeacher",
			column: "TeachersId");

		migrationBuilder.CreateIndex(
			name: "IX_RefreshSessions_TokenValue",
			table: "RefreshSessions",
			column: "TokenValue",
			unique: true);

		migrationBuilder.CreateIndex(
			name: "IX_Schedule_ClassroomId",
			table: "Schedule",
			column: "ClassroomId");

		migrationBuilder.CreateIndex(
			name: "IX_Schedule_SubjectName",
			table: "Schedule",
			column: "SubjectName");

		migrationBuilder.CreateIndex(
			name: "IX_Schools_RegisterUri",
			table: "Schools",
			column: "RegisterUri",
			unique: true);

		migrationBuilder.CreateIndex(
			name: "IX_StudentProfiles_ClassroomId",
			table: "StudentProfiles",
			column: "ClassroomId");

		migrationBuilder.CreateIndex(
			name: "IX_SubjectTeacher_TeacherId",
			table: "SubjectTeacher",
			column: "TeacherId");

		migrationBuilder.CreateIndex(
			name: "IX_SwapRequest_ScheduleId_RequestedSlotId",
			table: "SwapRequest",
			columns: new[ ] { "ScheduleId", "RequestedSlotId" },
			unique: true);

		migrationBuilder.CreateIndex(
			name: "IX_TeacherProfiles_TeacherId",
			table: "TeacherProfiles",
			column: "TeacherId",
			unique: true);

		migrationBuilder.CreateIndex(
			name: "IX_Teachers_ScheduleId",
			table: "Teachers",
			column: "ScheduleId");

		migrationBuilder.CreateIndex(
			name: "IX_Teachers_SchoolId",
			table: "Teachers",
			column: "SchoolId");

		migrationBuilder.AddForeignKey(
			name: "FK_AuditLog_Schedule_ScheduleId",
			table: "AuditLog",
			column: "ScheduleId",
			principalTable: "Schedule",
			principalColumn: "Id");

		migrationBuilder.AddForeignKey(
			name: "FK_Classrooms_TeacherProfiles_TeacherProfileId",
			table: "Classrooms",
			column: "TeacherProfileId",
			principalTable: "TeacherProfiles",
			principalColumn: "Id");
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropForeignKey(
			name: "FK_TeacherProfiles_AspNetUsers_Id",
			table: "TeacherProfiles");

		migrationBuilder.DropForeignKey(
			name: "FK_Classrooms_Schools_SchoolId",
			table: "Classrooms");

		migrationBuilder.DropForeignKey(
			name: "FK_Teachers_Schools_SchoolId",
			table: "Teachers");

		migrationBuilder.DropForeignKey(
			name: "FK_Teachers_Schedule_ScheduleId",
			table: "Teachers");

		migrationBuilder.DropTable(
			name: "AspNetRoleClaims");

		migrationBuilder.DropTable(
			name: "AspNetUserClaims");

		migrationBuilder.DropTable(
			name: "AspNetUserLogins");

		migrationBuilder.DropTable(
			name: "AspNetUserRoles");

		migrationBuilder.DropTable(
			name: "AspNetUserTokens");

		migrationBuilder.DropTable(
			name: "AuditLog");

		migrationBuilder.DropTable(
			name: "ClassroomTeacher");

		migrationBuilder.DropTable(
			name: "ExamSlotStudentProfile");

		migrationBuilder.DropTable(
			name: "LessonTeacher");

		migrationBuilder.DropTable(
			name: "RefreshSessions");

		migrationBuilder.DropTable(
			name: "SubjectTeacher");

		migrationBuilder.DropTable(
			name: "SwapRequest");

		migrationBuilder.DropTable(
			name: "AspNetRoles");

		migrationBuilder.DropTable(
			name: "ExamSlot");

		migrationBuilder.DropTable(
			name: "StudentProfiles");

		migrationBuilder.DropTable(
			name: "Lesson");

		migrationBuilder.DropTable(
			name: "AspNetUsers");

		migrationBuilder.DropTable(
			name: "Schools");

		migrationBuilder.DropTable(
			name: "Schedule");

		migrationBuilder.DropTable(
			name: "Classrooms");

		migrationBuilder.DropTable(
			name: "Subjects");

		migrationBuilder.DropTable(
			name: "Calendar");

		migrationBuilder.DropTable(
			name: "TeacherProfiles");

		migrationBuilder.DropTable(
			name: "Teachers");
	}
}
