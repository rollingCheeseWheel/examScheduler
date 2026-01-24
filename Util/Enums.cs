namespace Util;

public enum UserRoles
{
	Student,
	Teacher,
	Admin
}

public enum AutoLockIn
{
	FixedDate,
	TimeBeforeExamination,
}

public enum SlotFillingBehaviour
{
	RandomizeUnassigned,
	RandomizeUnassignedThenCompact,
	CompactAll
}

public enum AuditLogActor
{
	Student,
	Teacher,
	Admin,
	System
}