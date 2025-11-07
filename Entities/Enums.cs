namespace Entities;

public enum AutoLockIn
{
	FixedDate,
	TimeBeforeExamination,
}

[Flags]
public enum UserPermissions
{
	None = 0,
	Read = 1 << 0,    // 1
	Write = 1 << 1,   // 2
}

public enum UserProfileRoles
{
	Unknown,
	Student,
	Teacher,
}