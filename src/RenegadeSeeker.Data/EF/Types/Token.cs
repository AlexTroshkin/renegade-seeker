using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RenegadeSeeker.Data.EF.Types;

public class Token
{
    public Int64  Id              { get; set; }
    public Int64  ChainId         { get; set; }
    public String Address         { get; set; } = String.Empty;
    public String Symbol          { get; set; } = String.Empty;
    public Int16  Decimals        { get; set; }
    public String TransactionHash { get; set; } = String.Empty;
}

internal class TokenConfiguration : IEntityTypeConfiguration<Token>
{
    public void Configure(EntityTypeBuilder<Token> builder)
    {
        builder
            .HasKey(x => x.Id);

        builder
            .Property(x => x.Address)
            .HasMaxLength(42);

        builder
            .Property(x => x.Symbol)
            .HasMaxLength(16);

        builder
            .Property(x => x.TransactionHash)
            .HasMaxLength(66);
    }
}