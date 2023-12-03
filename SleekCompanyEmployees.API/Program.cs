using SleekCompanyEmployees.API.Extensions;
//https://stackup.hashnode.dev/connect-to-mongodb-redis-postgresql-docker-containers-from-aspnet-core
var app = WebApplication.CreateBuilder(args)
    .RegisterServices()
    .Build();

app.SetupMiddleware()
    .Run();