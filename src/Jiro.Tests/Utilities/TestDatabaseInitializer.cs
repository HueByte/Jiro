using Jiro.Core.Models;
using Jiro.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jiro.Tests.Utilities
{
    public class TestDatabaseInitializer
    {
        private readonly DbContextOptions<JiroContext> _dbContextOptions;

        public TestDatabaseInitializer()
        {
            _dbContextOptions = new DbContextOptionsBuilder<JiroContext>()
                .UseSqlite("DataSource=:memory:") // Use an in-memory SQLite database for testing
            .Options;
        }

        public JiroContext CreateDbContext()
        {
            var dbContext = new JiroContext(_dbContextOptions);
            dbContext.Database.OpenConnection();
            dbContext.Database.EnsureCreated();
            return dbContext;
        }

        public void SeedData()
        {
            using var dbContext = CreateDbContext();
            // Perform data seeding
            // Example: Add test entities to the database
            //dbContext.TestEntities.Add(new TestEntity { Id = 1, Name = "Entity 1" });
            //dbContext.TestEntities.Add(new TestEntity { Id = 2, Name = "Entity 2" });
            dbContext.SaveChanges();
        }

        public void CleanData()
        {
            using var dbContext = CreateDbContext();

            // Perform data cleanup
            // Example: Remove all test entities from the database
            //dbContext.TestEntities.RemoveRange(dbContext.TestEntities);
            dbContext.SaveChanges();
        }
    }
}
