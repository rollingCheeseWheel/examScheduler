var builder = DistributedApplication.CreateBuilder(args);

var postgresServer = builder.AddAzurePostgresFlexibleServer("database"); // server resource name
var postgresDb = postgresServer.AddDatabase("postgres");

var keyvault = builder.AddAzureKeyVault("keyvault");

builder.AddProject<Projects.examScheduler>("examscheduler")
	.WithReference(postgresDb)
	.WaitFor(postgresDb)
	.WithReference(keyvault)
	.WaitFor(keyvault);

builder.Build().Run();