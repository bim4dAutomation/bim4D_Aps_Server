using APS_Automation_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APS_Automation_Server.Data
{
    public class ApsDBContext : DbContext
    {
        public ApsDBContext(DbContextOptions<ApsDBContext> options) : base(options)
        {
        }

        public DbSet<user> users => Set<user>();
    }
}
