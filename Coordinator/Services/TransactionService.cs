using Coordinator.Models;
using Coordinator.Models.Contexts;
using Coordinator.Services.Abstracts;
using Microsoft.EntityFrameworkCore;

namespace Coordinator.Services
{
    public class TransactionService(TwoPhaseCommitContext _twoPhaseCommitContext, IHttpClientFactory _httpClientFactory) : ITransactionService
    {
        HttpClient _orderHttpClient = _httpClientFactory.CreateClient("OrderAPI");
        HttpClient _stockHttpClient = _httpClientFactory.CreateClient("StockAPI");
        HttpClient _paymentHttpClient = _httpClientFactory.CreateClient("PaymentAPI");

        public async Task<Guid> CreateTransactionAsync()
        {
            Guid transactionId = Guid.NewGuid();

            var nodes = await _twoPhaseCommitContext.Nodes.ToListAsync();
            nodes.ForEach(node => node.NodeStates = new List<NodeState>
            {
                new NodeState(transactionId)
                {
                    IsReady = Enums.ReadyType.Pending,
                    TransactionState = Enums.TransactionState.Pending,
                }
            });
            await _twoPhaseCommitContext.SaveChangesAsync();

            return transactionId;
        }

        public async Task PrepareServicesAsync(Guid TransactionId)
        {
            var transactionNodes = await _twoPhaseCommitContext.NodeStates
             .Include(ns => ns.Node)
             .Where(ns => ns.TransactionId == TransactionId)
             .ToListAsync();

            foreach (var transactionNode in transactionNodes)
            {
                try
                {
                    var response = await (transactionNode.Node.Name switch
                    {
                        "Order.API" => _orderHttpClient.GetAsync("ready"),
                        "Payment.API" => _paymentHttpClient.GetAsync("ready"),
                        "Stock.API" => _stockHttpClient.GetAsync("ready"),
                    });

                    var result = bool.Parse(await response.Content.ReadAsStringAsync());

                    transactionNode.IsReady = result ? Enums.ReadyType.Ready : Enums.ReadyType.Unready;
                }
                catch (Exception e)
                {
                    transactionNode.IsReady = Enums.ReadyType.Unready;
                }
            }

            await _twoPhaseCommitContext.SaveChangesAsync();
        }

        public async Task<bool> CheckReadyServicesAsync(Guid TransactionId)
        => (await _twoPhaseCommitContext.NodeStates
             .Where(ns => ns.TransactionId == TransactionId)
             .ToListAsync()).TrueForAll(x => x.IsReady == Enums.ReadyType.Ready);

        public async Task CommitAsync(Guid TransactionId)
        {
            var transactionNodes = await _twoPhaseCommitContext.NodeStates
             .Where(ns => ns.TransactionId == TransactionId)
             .Include(ns => ns.Node)
             .ToListAsync();

            foreach (var transactionNode in transactionNodes)
            {
                try
                {
                    var response = await (transactionNode.Node.Name switch
                    {
                        "Order.API" => _orderHttpClient.GetAsync("commit"),
                        "Payment.API" => _paymentHttpClient.GetAsync("commit"),
                        "Stock.API" => _stockHttpClient.GetAsync("commit"),
                    });

                    var result = bool.Parse(await response.Content.ReadAsStringAsync());

                    transactionNode.TransactionState = result ? Enums.TransactionState.Done : Enums.TransactionState.Abort;
                }
                catch (Exception e)
                {
                    transactionNode.TransactionState = Enums.TransactionState.Abort;
                }
            }

            await _twoPhaseCommitContext.SaveChangesAsync();
        }
        public async Task<bool> CheckTransactionStateServicesAsync(Guid TransactionId)
        => (await _twoPhaseCommitContext.NodeStates
             .Where(ns => ns.TransactionId == TransactionId)
             .ToListAsync()).TrueForAll(x => x.TransactionState == Enums.TransactionState.Done);
        public async Task RollBackAsync(Guid TransactionId)
        {
            var transactionNodes = await _twoPhaseCommitContext.NodeStates
             .Where(ns => ns.TransactionId == TransactionId)
             .Include(ns => ns.Node)
             .ToListAsync();

            foreach (var transactionNode in transactionNodes)
            {

                try
                {
                    if (transactionNode.TransactionState == Enums.TransactionState.Done)
                        _ = await (transactionNode.Node.Name switch
                        {
                            "Order.API" => _orderHttpClient.GetAsync("rollback"),
                            "Payment.API" => _paymentHttpClient.GetAsync("rollback"),
                            "Stock.API" => _stockHttpClient.GetAsync("rollback"),
                        });

                    transactionNode.TransactionState = Enums.TransactionState.Abort;
                }
                catch (Exception e)
                {
                    transactionNode.TransactionState = Enums.TransactionState.Abort;
                }
            }

            await _twoPhaseCommitContext.SaveChangesAsync();
        }
    }
}
