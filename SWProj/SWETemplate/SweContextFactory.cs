using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SWETemplate.Models;

namespace SWETemplate
{
    public class SweContextFactory : IDesignTimeDbContextFactory<SweContext>
    {
        public SweContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<SweContext>();
            var connectionString = configuration.GetConnectionString("SWE");

            optionsBuilder.UseSqlServer(connectionString);

            return new SweContext(optionsBuilder.Options);
        }
    }
}
