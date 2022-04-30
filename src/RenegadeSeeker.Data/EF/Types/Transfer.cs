using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RenegadeSeeker.Data.EF.Types;

public class Transfer
{
    public Int64  ChainId         { get; set; }
    public Int64  Height          { get; set; }
    public Int64  LogIndex        { get; set; }
    public String TransactionHash { get; set; } = String.Empty;
    public String From            { get; set; } = String.Empty;
    public String To              { get; set; } = String.Empty;
    public Byte[] Amount          { get; set; } = new Byte[32];
    public Int64  TokenId         { get; set; }
}

internal class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder
            .HasKey(x => new
            {
                x.ChainId,
                x.TransactionHash,
                x.LogIndex
            });

        builder
            .Property(x => x.TransactionHash)
            .HasMaxLength(66);

        builder
            .Property(x => x.From)
            .HasMaxLength(42);

        builder
            .Property(x => x.To)
            .HasMaxLength(42);

        builder
            .HasOne<Token>()
            .WithMany()
            .HasForeignKey(x => x.TokenId);
    }
}