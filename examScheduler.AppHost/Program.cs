var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("database") // container name
	.WithLifetime(ContainerLifetime.Persistent)
	.WithDataVolume()
	.WithHostPort(5432)
	.AddDatabase("postgres"); // connection string name

builder.AddProject<Projects.examScheduler>("examscheduler")
	.WithReference(postgres)
	.WaitFor(postgres);

builder.Build().Run();
