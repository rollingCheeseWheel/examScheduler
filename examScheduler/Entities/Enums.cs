namespace examScheduler.Entities;

public enum AutoLockIn
{
	OnExamination = 0,
	FixedDate = 1,
	TimeBeforeExamination = 2,
}

[Flags]
public enum Permission
{
	None = 0,
	Read = 1 << 0,    // 1
	Write = 1 << 1,   // 2
}