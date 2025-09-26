using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VoiceInputAssistant.Infrastructure.Data;

/// <summary>
/// Factory for creating ApplicationDbContext at design time for EF migrations
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use SQLite for migrations (can be overridden at runtime)
        optionsBuilder.UseSqlite("Data Source=voiceinputassistant.db");
        
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}