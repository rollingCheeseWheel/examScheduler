using examScheduler.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

//Local
/*var postgres = builder.AddPostgres(ResourceNames.DBResourceName) // container name
	.WithLifetime(ContainerLifetime.Persistent)
	.WithDataVolume()
	.WithHostPort(5432)
	.AddDatabase(ResourceNames.DBName);

builder.AddProject<Projects.examScheduler>("examscheduler")
	.WithReference(postgres)
	.WaitFor(postgres);*/

//Azure
var postgres = builder.AddAzurePostgresFlexibleServer(ResourceNames.DBResourceName)
	.AddDatabase(ResourceNames.DBName);

var keyvault = builder.AddAzureKeyVault(ResourceNames.KeyVault);

builder.AddProject<Projects.examScheduler>("examscheduler")
	.WithReference(postgres)
	.WithReference(keyvault)
	.WaitFor(postgres)
	.WaitFor(keyvault);

builder.Build().Run();