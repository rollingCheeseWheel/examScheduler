namespace Util;

public enum UserRoles
{
	Student,
	Teacher,
	//Admin,
	//Parent
}

public enum AutoLockIn
{
	FixedDate,
	TimeBeforeExamination,
}

public enum SlotFillingBehaviour
{
	RandomizeUnassigned,
	//RandomizeUnassignedThenCompact,
	//CompactAll
}

public enum AuditLogActor
{
	Student,
	Teacher,
	System
}

public enum AuditLogTarget
{
	Student,
	Schedule,
	ExamSlot,
	SwapRequest
}