var builder = DistributedApplication.CreateBuilder(args);

var (resourceName, dbName) = ("database", "postgres");

if (Environment.GetEnvironmentVariable("AZURE_ENV_NAME") is null) // runs locally
{
	Console.WriteLine("Being run locally");
	var postgres = builder.AddPostgres(resourceName) // container name
		.WithLifetime(ContainerLifetime.Persistent)
		.WithDataVolume()
		.WithHostPort(5432)
		.AddDatabase(dbName);

	builder.AddProject<Projects.examScheduler>("examscheduler")
		.WithReference(postgres)
		.WaitFor(postgres);
}
else
{
	Console.WriteLine("Being built for azure");
	var postgres = builder.AddAzurePostgresFlexibleServer(resourceName).AddDatabase(dbName);

	var keyvault = builder.AddAzureKeyVault("keyvault");

	builder.AddProject<Projects.examScheduler>("examscheduler")
		.WithReference(postgres)
		.WaitFor(postgres)
		.WithReference(keyvault)
		.WaitFor(keyvault);
}

builder.Build().Run();