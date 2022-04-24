using RenegadeSeeker.Services.TokenTransactions;
using System.Threading.Channels;

namespace RenegadeSeeker.App.HostedServices
{
    public class BinancePullerHostedService : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;

        public BinancePullerHostedService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = new Dictionary<String, String>
            {
                { "rpcUrl"      , "https://rpc.ankr.com/eth"                   },
                { "tokenAddress", "0xdAC17F958D2ee523a2206206994597C13D831ec7" },
                { "fromHeight"  , "4634748"                                    },
            };

            var pullerChannel = Channel.CreateUnbounded<Transaction>();
            var puller        = new BinanceScPuller();

            Task.Factory
                .StartNew(async () =>
                {
                    await puller.RunAsync(settings, pullerChannel.Writer, CancellationToken.None);
                })
                .Forget();

            while (stoppingToken.IsCancellationRequested is false)
            {
                var transaction = await pullerChannel.Reader.ReadAsync(CancellationToken.None);
                Console.WriteLine($"{transaction.Hash} #{transaction.Height} | {Nethereum.Util.UnitConversion.Convert.FromWei(transaction.Amount, 6)} USDT {transaction.From} -> {transaction.To}");
            }
        }
    }
}
