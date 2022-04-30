using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using RenegadeSeeker.Base;
using System.Numerics;
using System.Threading.Channels;

namespace RenegadeSeeker.Services;

public static class TransfersObserver
{
    public record Progress(
        BigInteger CurrentHeight,
        BigInteger ChainHeight,
        UInt16     Percentage);

    public record Transfer(
        BigInteger Height,
        String     TransactionHash,
        BigInteger LogIndex,
        String     From,
        String     To,
        BigInteger Amount);

    public record ObserveCommand(
        ChannelWriter<Transfer> TransferWriter,
        ChannelWriter<Progress> ProgressWriter,
        String                  RpcUrl,
        String                  TokenAddress,
        BigInteger              FromHeight);

    public static async ValueTask<Either<Nothing>> ObserveAsync(ObserveCommand command, CancellationToken cancellationToken = default)
    {
        var web3          = new Web3(command.RpcUrl);
        var chainId       = await web3.Eth.ChainId.SendRequestAsync();
        var tokenAddress  = command.TokenAddress;
        var fromHeight    = command.FromHeight;
        var toHeight      = fromHeight + 1_000;
        var tokenContract = web3.Eth.ERC20.GetERC20ContractService(tokenAddress);
        var transferEvent = tokenContract.GetTransferEvent();
        var chainHeight   = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();

        _ = Task.Run(async () =>
        {
            while (cancellationToken.IsCancellationRequested is false)
            {
                var progress    = new Progress(
                    CurrentHeight : fromHeight,
                    ChainHeight   : chainHeight,
                    Percentage    : (UInt16)(fromHeight * 100 / chainHeight));

                await command.ProgressWriter.WriteAsync(progress);
                await Task.Delay(5_000);

                try
                {
                    chainHeight = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception occured: {e.ToString()}");
                }
            }
        }, cancellationToken);

        _ = Task.Run((async () =>
        {
            while (cancellationToken.IsCancellationRequested is false)
            {
                var filter = transferEvent.CreateFilterInput(
                    fromBlock : new BlockParameter(new HexBigInteger(fromHeight)),
                    toBlock   : new BlockParameter(new HexBigInteger(toHeight)));

                fromHeight = toHeight + 1;
                toHeight   = toHeight + 1_000 > chainHeight
                    ? toHeight + (chainHeight - toHeight)
                    : toHeight + 1000;

                if (fromHeight >= toHeight)
                {
                    await Task.Delay(5_000);
                    continue;
                }

                var transfers        = await transferEvent.GetAllChangesAsync(filter);
                var orderedTransfers = transfers
                    .OrderBy(transfer => transfer.Log.BlockNumber.Value)
                    .ThenBy(transfer => transfer.Log.TransactionIndex.Value)
                    .Select(transfer => new Transfer(
                        Height          : transfer.Log.BlockNumber,
                        TransactionHash : transfer.Log.TransactionHash,
                        LogIndex        : transfer.Log.LogIndex,
                        From            : transfer.Event.From,
                        To              : transfer.Event.To,
                        Amount          : transfer.Event.Value
                        ))
                    .ToList();

                foreach (var transfer in orderedTransfers)
                {
                    await command.TransferWriter.WriteAsync(transfer, CancellationToken.None);
                }
            }
        }), cancellationToken);

        return nothing;
    }
}
