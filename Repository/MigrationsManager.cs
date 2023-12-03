﻿using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
///kovndsjndfjnfsknfsf
namespace Repository 
{
    public static class MigrationsManager
    {
        private static int _numberOfRetries;

        public static IHost MigrateDatabase(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                using var appContext = scope.ServiceProvider.GetRequiredService<RepositoryContext>();
                if (appContext.Database.GetPendingMigrations().Any())
                {
                    Console.WriteLine($"There are pending migrations application will not start. Make sure migrations are ran.");
                    try
                    {
                        appContext.Database.Migrate();
                    }
                    catch (SqlException)
                    {
                        if (_numberOfRetries < 6)
                        {
                            Thread.Sleep(10000);

                            _numberOfRetries++;
                            Console.WriteLine($"The server was not found or was not accessible. Retrying... #{_numberOfRetries}");

                            MigrateDatabase(host);
                        }

                        throw;
                    }

                }
               
            }

            return host;
        }
    }
}