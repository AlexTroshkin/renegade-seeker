using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using RenegadeSeeker.Base;
using System.Numerics;
using System.Threading.Channels;

namespace RenegadeSeeker.Services.TokenTransactions;

public class Transaction
{
    public String     Hash   { get; set; } = String.Empty;
    public BigInteger Height { get; set; }
    public BigInteger Index  { get; set; }
    public String     From   { get; set; } = String.Empty;
    public String     To     { get; set; } = String.Empty;
    public BigInteger Amount { get; set; }
}

internal interface IPuller
{
    public ValueTask<Either<Nothing>> RunAsync(Dictionary<String, String> settings, ChannelWriter<Transaction> @out, CancellationToken cancellationToken = default);
}


public class BinanceScPuller : IPuller
{
    private const UInt16 HEIGHT_WINDOW_SIZE = 1_000;

    public async ValueTask<Either<Nothing>> RunAsync(Dictionary<String, String> settings, ChannelWriter<Transaction> @out, CancellationToken cancellationToken)
    {
        var containsRpcUrl = settings.ContainsKey("rpcUrl");
        if (containsRpcUrl is false)
        {
            return new Error(
                id          : "32e7d72b-9e1d-4616-9862-1578ad363d3e",
                description : "неуказан RPC URL");
        }

        var rpcUrl = settings["rpcUrl"];

        var tokenAddress = settings.GetValueOrDefault("tokenAddress");
        if (tokenAddress is null)
        {
            return new Error(
                id          : "bca443fa-5713-4a56-b769-d3754c3a4207",
                description : "неуказан адрес токена");
        }

        var fromHeightString = settings.GetValueOrDefault("fromHeight");
        var fromHeight       = (BlockParameter?) null;
        var toHeight         = (BlockParameter?) null;

        if (fromHeightString is not null)
        {
            var parsed = BigInteger.TryParse(fromHeightString, out var fromHeightInt);
            if (parsed)
            {                
                fromHeight = new BlockParameter(fromHeightInt.ToHexBigInteger());
                toHeight   = new BlockParameter((fromHeightInt + HEIGHT_WINDOW_SIZE).ToHexBigInteger());
            }
            else
            {
                return new Error(
                    id          : "44d803e7-6f8c-4987-bc6b-33f39d4722c9",
                    description : "номер последнего обработанного блока содержит невалидное значение");
            }
        }
        else
        {
            return new Error(
                id          : "ae303e9d-3ca6-4156-8e4d-559117dfcaad",
                description : "отсутствует номер начального блока");
        }

        var w3 = new Web3(rpcUrl);

        var tokenContract = w3.Eth.ERC20.GetERC20ContractService(tokenAddress);        
        var transferEvent = tokenContract.GetTransferEvent();
        var filter        = transferEvent.CreateFilterInput(
            fromBlock : fromHeight,
            toBlock   : toHeight);
        
        while (cancellationToken.IsCancellationRequested is false)
        {
            var transfers = await transferEvent.GetAllChangesAsync(filter);
            var orderedTransfers = transfers
                .OrderBy(transfer => transfer.Log.BlockNumber.Value)
                .ThenBy(transfer => transfer.Log.TransactionIndex.Value)
                .ToList();


            foreach (var transfer in orderedTransfers)
            {
                var transaction = new Transaction
                {
                    Amount = transfer.Event.Value,
                    From   = transfer.Event.From,
                    Hash   = transfer.Log.TransactionHash,
                    Height = transfer.Log.BlockNumber.Value,
                    Index  = transfer.Log.TransactionIndex.Value,
                    To     = transfer.Event.To
                };

                try
                {
                    await @out.WriteAsync(transaction, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    return nothing;
                }
            }

            filter.FromBlock = new BlockParameter((filter.FromBlock.BlockNumber.Value + HEIGHT_WINDOW_SIZE).ToHexBigInteger());
            filter.ToBlock   = new BlockParameter((filter.FromBlock.BlockNumber.Value + HEIGHT_WINDOW_SIZE).ToHexBigInteger());
        }

        return nothing;
    }
}