using Microsoft.EntityFrameworkCore;
using RenegadeSeeker.Data.EF;
using RenegadeSeeker.Data.EF.Types;

namespace RenegadeSeeker.App.HostedServices
{
    public class TokenSeeder : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;

        public TokenSeeder(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = serviceProvider.CreateScope();

            var db     = scope.ServiceProvider.GetRequiredService<Db>();
            var tokens = await db.Tokens.ToListAsync(stoppingToken);

            var requiredTokens = new List<Token>
            {
                new Token
                {
                    Address         = "0xc17fbe1d709ddf6c0b6665dd0591046815ac7554",
                    ChainId         = 1,
                    Decimals        = 18,
                    Symbol          = "POL",
                    TransactionHash = "0xf7f732767c7cf6780fb3b538d2931bdc4c022a6c1a615621d53ea5fc96b715cd"
                },

                new Token
                {
                    Address         = "0x273a4ffceb31b8473d51051ad2a2edbb7ac8ce02",
                    ChainId         = 56,
                    Decimals        = 18,
                    Symbol          = "POL",
                    TransactionHash = "0x6fc822275ad90d59f3cebf69678c60f3bb8edc46b1f2130f18aac5940183fa20"
                },

                new Token
                {
                    Address         = "0x97513e975a7fA9072c72C92d8000B0dB90b163c5",
                    ChainId         = 128,
                    Decimals        = 18,
                    Symbol          = "POL",
                    TransactionHash = "0x5b2963f50878fd7e2af6ced54ee959cd2de288fc7546db453b5d75536b43ae63",
                }
            };

            var tokensToAdd = requiredTokens
                .FindAll(x => tokens.Any(t => t.Address == x.Address && t.ChainId == x.ChainId));

            if (tokensToAdd.Count != 0)
            {
                      db.AddRange(tokensToAdd);
                await db.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
