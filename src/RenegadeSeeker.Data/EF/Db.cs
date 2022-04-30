using Microsoft.EntityFrameworkCore;
using RenegadeSeeker.Data.EF.Types;

namespace RenegadeSeeker.Data.EF;

public class Db : DbContext
{
    public Db()
    {
        ChangeTracker.QueryTrackingBehavior    = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public Db(DbContextOptions<Db> options) : base(options)
    {        
        ChangeTracker.QueryTrackingBehavior    = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(Db).Assembly);
    }

    public DbSet<Token>    Tokens    { get; set; }
    public DbSet<Transfer> Transfers { get; set; }
}
