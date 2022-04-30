using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RenegadeSeeker.Data.EF;

public class DbDesignTimeFactory : IDesignTimeDbContextFactory<Db>
{
    public Db CreateDbContext(String[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<Db>();
            optionsBuilder.UseNpgsql(
                connectionString: "Server=localhost;Port=5432;Database=RenegadeSeeker;User ID=postgres;Password=742698513;");

        return new Db(optionsBuilder.Options);
    }
}
