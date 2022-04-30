using Microsoft.EntityFrameworkCore;
using RenegadeSeeker.Data.EF;
using RenegadeSeeker.Services;
using System.Numerics;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDbContext<Db>(options => options
        .UseNpgsql("Server=localhost;Port=5432;Database=RenegadeSeeker;User ID=postgres;Password=742698513")
        .EnableSensitiveDataLogging());

//builder.Services
//    .AddHostedService<TokenSeeder>()
//    .AddHostedService<BinancePullerHostedService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

using var scope = app.Services.CreateScope();

var db     = scope.ServiceProvider.GetRequiredService<Db>();
var tokens = await db.Tokens.ToListAsync();

var rpc = new Dictionary<Int64, String>
{
    { 1  , "https://rpc.ankr.com/eth"           },
    { 56 , "https://bsc-dataseed.binance.org"  },
    { 128, "https://http-mainnet.hecochain.com" }
};

var heights = new Dictionary<(Int64, String), BigInteger>
{
    { (1  , "0xc17fbe1d709ddf6c0b6665dd0591046815ac7554"), new BigInteger(12496531) },
    { (56 , "0x273a4ffceb31b8473d51051ad2a2edbb7ac8ce02"), new BigInteger(9930668 ) },
    { (128, "0x97513e975a7fA9072c72C92d8000B0dB90b163c5"), new BigInteger(10281490) }
};

var observationTasks = new List<Task>();

foreach (var token in tokens)
{
    var cts              = new CancellationTokenSource();
    var transfersChannel = Channel.CreateUnbounded<TransfersObserver.Transfer>();
    var progressChannel  = Channel.CreateUnbounded<TransfersObserver.Progress>();

    var rpcUrl      = rpc[token.ChainId];
    var fromHeight  = heights[(token.ChainId, token.Address)];

    var observeCmd  = new TransfersObserver.ObserveCommand(
        transfersChannel.Writer, 
        progressChannel.Writer, 
        rpcUrl,
        token.Address, 
        fromHeight);

    var observation = await TransfersObserver.ObserveAsync(observeCmd, cts.Token);
    var observationTask = Task.Run(async () =>
    {
        while (true)
        {
            var transfers = new List<TransfersObserver.Transfer>();

            while (transfersChannel.Reader.Count != 0)
            {
                var transfer = await transfersChannel.Reader.ReadAsync();
                    transfers.Add(transfer);
            }

            var progress = await progressChannel.Reader.ReadAsync();

            Console.WriteLine($"Chain #{token.ChainId} -> {progress.CurrentHeight} / {progress.ChainHeight} | {progress.Percentage}%");
            Console.WriteLine($"Received {transfers.Count} transfers");
            Console.WriteLine("-------------------------------------------------------------");

            db.Transfers.AddRange(transfers.ConvertAll(x => new RenegadeSeeker.Data.EF.Types.Transfer
            {
                Amount          = x.Amount.ToByteArray(),
                ChainId         = token.ChainId,
                From            = x.From,
                Height          = (Int64)x.Height,
                To              = x.To,
                TransactionHash = x.TransactionHash,
                TokenId         = token.Id,
                LogIndex        = (Int64)x.LogIndex
            }));

            await db.SaveChangesAsync(CancellationToken.None);

            if (progress.CurrentHeight >= progress.ChainHeight)
            {
                cts.Cancel();
                Console.WriteLine($"Chain #{token.ChainId} sync completed");
            }
        }
    });

    observationTasks.Add(observationTask);
}

await Task.WhenAll(observationTasks);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");;

app.Run();
