var builder = DistributedApplication.CreateBuilder(args);

var (resourceName, dbName) = ("database", "postgres");

/* Local */
var postgres = builder.AddPostgres(resourceName) // container name
	.WithLifetime(ContainerLifetime.Persistent)
	.WithDataVolume()
	.WithHostPort(5432)
	.AddDatabase(dbName);

builder.AddProject<Projects.examScheduler>("examscheduler")
	.WithReference(postgres)
	.WaitFor(postgres);

/* Azure */
/*var postgres = builder.AddAzurePostgresFlexibleServer(resourceName).AddDatabase(dbName);

builder.AddProject<Projects.examScheduler>("examscheduler")
	.WithReference(postgres)
	.WaitFor(postgres);*/

builder.Build().Run();