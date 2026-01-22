using examScheduler.AppHost;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { EnableResourceLogging = true });

//Local
IResourceBuilder<PostgresDatabaseResource> postgres = builder.AddPostgres(ResourceNames.DBResourceName) // container name
	.WithLifetime(ContainerLifetime.Persistent)
	.WithDataVolume()
	.WithHostPort(5432)
	.AddDatabase(ResourceNames.DBName);

builder.AddProject<Projects.examScheduler>(ResourceNames.ExamSchedulerName)
	.WithReference(postgres)
	.WaitFor(postgres);

//Azure
/*var postgres = builder.AddAzurePostgresFlexibleServer(ResourceNames.DBResourceName)
	.AddDatabase(ResourceNames.DBName);

var keyvault = builder.AddAzureKeyVault(ResourceNames.KeyVault);

builder.AddProject<Projects.examScheduler>(ResourceNames.ExamSchedulerName)
	.WithReference(postgres)
	.WithReference(keyvault)
	.WaitFor(postgres)
	.WaitFor(keyvault);*/

builder.Build().Run();