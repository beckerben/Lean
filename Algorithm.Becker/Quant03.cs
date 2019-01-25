using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.Becker
{
    public class Quant03 : QCAlgorithm, IRegressionAlgorithmDefinition
    {

        //constants for configuraing bot
        const decimal buySizeUSD = 50;
        const decimal stepUpPct = 1.020m;
        const decimal stepDownPct = 0.990m;
        const decimal seedDepthPct = 0.85m;
        const decimal lossPct = 0.990m;  //todo: not implemented

        //variables for trading
        const string symbolName = "ETHUSD";
        private Symbol symbol = QuantConnect.Symbol.Create(symbolName, SecurityType.Crypto, Market.GDAX);
        private readonly List<OrderTicket> openLimitOrders = new List<OrderTicket>();

        private bool initialize = true;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 4, 4);  //Set Start Date
            SetEndDate(2018, 4, 5);    //Set End Date
            SetCash(1000);            //Set Strategy Cash
            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);
            DefaultOrderProperties = new GDAXOrderProperties { PostOnly = true };
            AddCrypto(symbolName, Resolution.Minute);
        }

		/// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {

            //on initialize, we will spin up seeding the buy orders
            if (data.Bars[symbol].Close > 0)
            {
                if (initialize)
                {
                    initialize = false;
                    PlaceBuyOrders(data.Bars[symbol]);
                }
                else
                {
                    CheckSeedDepth(data.Bars[symbol]);
                    RealizeLosses();
                }
            }
        }

        /// <summary>
        /// Order events are triggered on order status changes. There are many order events including non-fill messages.
        /// </summary>
        /// <param name="orderEvent">OrderEvent object with details about the order status</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Canceled)
            {
                //todo: resubmit moving higher / lower, likely probably rejected due to price
            }

            if (orderEvent.Status.IsFill())
            {
                //Debug(Time + ": Filled: " + Transactions.GetOrderById(orderEvent.OrderId));
                //place the opposite order
                if (orderEvent.Direction == OrderDirection.Buy) 
                {
                    PlaceSell(TruncateDecimal(orderEvent.FillPrice * stepUpPct,2), -1 * orderEvent.FillQuantity);
                }
                if (orderEvent.Direction == OrderDirection.Sell)
                {
                    PlaceBuy(TruncateDecimal(orderEvent.FillPrice * stepDownPct, 2));
                }
            }
        }


        #region "helpers"
       

        private void CheckSeedDepth(TradeBar bar)
        {
            //make sure there are buy orders down to the limit
            var lowestBuy = Transactions.GetOpenOrders(x => x.Direction == OrderDirection.Buy && x.Type == OrderType.Limit)
                    .Where(x => x.Symbol == symbolName)
                    .OrderByDescending(x => x.Price)
                    .FirstOrDefault();

            var highestBuy = Transactions.GetOpenOrders(x => x.Direction == OrderDirection.Buy && x.Type == OrderType.Limit)
                    .Where(x => x.Symbol == symbolName)
                    .OrderBy(x => x.Price)
                    .FirstOrDefault();

            if (lowestBuy.Price > (bar.Close * seedDepthPct * stepDownPct))
            {
                //need to place more buy(s)
                PlaceBuyOrders(bar, lowestBuy.Price);
            }
        }

        private void RealizeLosses()
        {
            //todo: code this
        }

        private void PlaceBuyOrders(TradeBar bar, decimal? lowestBuyPrice = null)
        {
            decimal nextBuyPrice;
            if (!lowestBuyPrice.HasValue)
            {
                nextBuyPrice = bar.Close * stepDownPct;
            }
            else
            {
                nextBuyPrice = lowestBuyPrice.Value * stepDownPct;
            }

            bool buyOrdersDone = false;
            while (!buyOrdersDone)
            {
                if (nextBuyPrice >= (bar.Close * seedDepthPct))
                {
                    PlaceBuy(TruncateDecimal(nextBuyPrice,2));
                    
                }
                else
                {
                    buyOrdersDone = true;
                }
                nextBuyPrice = nextBuyPrice * stepDownPct;
            }
        }
        

        private void PlaceBuy(decimal buyPrice)
        {
            //Debug("Trigger buy " + Time.ToString("u") + " " + bar);
            decimal qty = decimal.Divide(buySizeUSD, buyPrice);
            var newTicket = LimitOrder(symbol, qty, buyPrice);
            openLimitOrders.Add(newTicket);
        }

        private void PlaceSell(decimal sellPrice, decimal qty)
        {
            //Debug("Trigger sell " + Time.ToString("u") + " " + bar);
            var newTicket = LimitOrder(symbol, qty, sellPrice);
            openLimitOrders.Add(newTicket);
        }

        public decimal TruncateDecimal(decimal value, int precision)
        {
            decimal step = (decimal)Math.Pow(10, precision);
            decimal tmp = Math.Truncate(step * value);
            return tmp / step;
        }

        #endregion //helpers

        #region "standard properties"

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "263.153%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.663%"},
            {"Sharpe Ratio", "4.41"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.007"},
            {"Beta", "76.118"},
            {"Annual Standard Deviation", "0.192"},
            {"Annual Variance", "0.037"},
            {"Information Ratio", "4.354"},
            {"Tracking Error", "0.192"},
            {"Treynor Ratio", "0.011"},
            {"Total Fees", "$3.26"}
        };

        #endregion

    }
}


