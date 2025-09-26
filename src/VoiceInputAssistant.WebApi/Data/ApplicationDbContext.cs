using Microsoft.EntityFrameworkCore;
using VoiceInputAssistant.WebApi.Models;

namespace VoiceInputAssistant.WebApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Add your DbSets here. For example:
        // public DbSet<User> Users { get; set; }
    }
}
