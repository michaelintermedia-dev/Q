var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Q_WebAPI>("q-webapi");

builder.Build().Run();
