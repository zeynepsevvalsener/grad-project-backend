using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Tests;

public static class TestDbFactory
{
    public static AppDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
