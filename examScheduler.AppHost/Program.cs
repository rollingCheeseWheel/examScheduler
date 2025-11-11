var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddAzurePostgresFlexibleServer("postgres") // container name
	//.WithLifetime(ContainerLifetime.Persistent)
	//.WithDataVolume()
	//.WithHostPort(5432)
	.AddDatabase("postgres"); // connection string name

var keyvault = builder.AddAzureKeyVault("keyvault");

builder.AddProject<Projects.examScheduler>("examscheduler")
	.WithReference(postgres)
	.WaitFor(postgres)
	.WithReference(keyvault)
	.WaitFor(keyvault);

builder.Build().Run();
