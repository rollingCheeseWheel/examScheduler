var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.examScheduler>("examscheduler");

builder.Build().Run();
