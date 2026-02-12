var builder = DistributedApplication.CreateBuilder(args);


var postgres = builder.AddPostgres("postgres")
.WithDataVolume()
.WithPgAdmin(c =>
{
    c.WithLifetime(ContainerLifetime.Persistent);
    c.WithHostPort(52651);
})
.WithHostPort(5432)
.WithLifetime(ContainerLifetime.Persistent)
.AddDatabase("q");

builder.AddProject<Projects.Q_WebAPI>("q-webapi")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.Build().Run();
