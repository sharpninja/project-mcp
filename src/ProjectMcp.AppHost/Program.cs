using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
var isVerifyDb = args.Length > 0 && args[0] == "verify-db";

var postgres = builder.AddPostgres("postgres");
var projectDb = postgres.AddDatabase("projectmcp");

var webappBuilder = builder.AddProject<Projects.ProjectMcp_WebApp>("webapp")
    .WithReference(projectDb, connectionName: "DefaultConnection");

if (isVerifyDb)
    webappBuilder.WithArgs("verify-db");

builder.Build().Run();
