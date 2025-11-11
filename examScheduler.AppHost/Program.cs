var builder = DistributedApplication.CreateBuilder(args);

var (resourceName, dbName) = ("database", "postgres");

if (Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") is null) // runs locally
{

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
	var postgres = builder.AddAzurePostgresFlexibleServer(resourceName).AddDatabase(dbName);

	var keyvault = builder.AddAzureKeyVault("keyvault");

	builder.AddProject<Projects.examScheduler>("examscheduler")
		.WithReference(postgres)
		.WaitFor(postgres)
		.WithReference(keyvault)
		.WaitFor(keyvault);
}

builder.Build().Run();