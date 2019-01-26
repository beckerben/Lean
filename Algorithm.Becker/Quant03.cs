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
    class OrderEntry
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public decimal Qty { get; set; }
    }

    public class Quant03 : QCAlgorithm, IRegressionAlgorithmDefinition
    {

        #region "constants and variables"
        //constants for configuraing bot
        //important: changing these constants after the strat has been running could have ill effect on the orders on the book already
        const decimal orderEntryMaxPrice = 1500; //the max price the canned orderbook will go to
        const decimal stepPct = 0.0025m; //the price change percentage for individual entries
        
        //variables for trading
        const string symbolName = "ETHUSD";
        private decimal realizeLossPct = 0.990m;  //not implemented, will be the sell order bail threshold
        private decimal buySizeUSD = 20; //the position size in USD
        private int sellSteps = 4; //the number of steps in the orderbook to go up to place a sell
        private int buySteps = 2;  //the number of steps in the order book to place buys
        private decimal seedDepthPct = 0.80m;//the percentage down from the current tick to have buy orders
        private Symbol symbol = QuantConnect.Symbol.Create(symbolName, SecurityType.Crypto, Market.GDAX);
        private readonly List<OrderTicket> openLimitOrders = new List<OrderTicket>();
        private List<OrderEntry> OrderEntries = new List<OrderEntry>();
        #endregion

        #region "events"
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 4, 4); //Set Start Date
            SetEndDate(2018, 4, 4);   //Set End Date
            SetCash(10000);            //Set Strategy Cash
            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);
            DefaultOrderProperties = new GDAXOrderProperties { PostOnly = true };
            AddCrypto(symbolName, Resolution.Minute);

            SetupOrderEntries();

        }

		/// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (data.Bars[symbol].Close > 0)
            {
                Debug(Time + " OnData " + data.Bars[symbol].Close.ToString());
                CheckBuyDepth(data.Bars[symbol]);
                //todo: code realizelosses to close out unrealized losses after some % loss or age?
                //RealizeLosses();
            }
        }

        /// <summary>
        /// Order events are triggered on order status changes. There are many order events including non-fill messages.
        /// </summary>
        /// <param name="orderEvent">OrderEvent object with details about the order status</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            //if (orderEvent.Status == OrderStatus.Canceled)
            //{
            //    //todo: resubmit moving higher / lower, likely probably rejected due to price
            //}

            if (orderEvent.Status == OrderStatus.Filled)
            {
                Debug(Time + " " + orderEvent.Direction + " Filled: " + orderEvent + " ---- Transaction ---- " + Transactions.GetOrderById(orderEvent.OrderId));
                //place the opposite order
                if (orderEvent.Direction == OrderDirection.Buy) 
                {
                    
                    var buyEntry = OrderEntries.Where(x => x.Id == Convert.ToInt32(Transactions.GetOrderById(orderEvent.OrderId).Tag)).FirstOrDefault();
                    var sellEntry = OrderEntries.Where(x => x.Id == buyEntry.Id + buySteps).FirstOrDefault();
                    if (sellEntry != null)
                    {
                        PlaceOrder(sellEntry.Price,-1*orderEvent.AbsoluteFillQuantity,sellEntry.Id.ToString());
                    }
                    
                }
            }
        }

        #endregion

        #region "helpers"

        private void CheckBuyDepth(TradeBar bar)
        {
            //make sure there are buy orders down to the limit
            var lowestOpenBuy = Transactions.GetOpenOrders(x => x.Direction == OrderDirection.Buy && x.Type == OrderType.Limit)
                    .Where(x => x.Symbol == symbolName)
                    .OrderBy(x => Convert.ToInt32(x.Tag))
                    .FirstOrDefault();

            var highestOpenBuy = Transactions.GetOpenOrders(x => x.Direction == OrderDirection.Buy && x.Type == OrderType.Limit)
                    .Where(x => x.Symbol == symbolName)
                    .OrderByDescending(x => Convert.ToInt32(x.Tag))
                    .FirstOrDefault();


            var lowestBuyEntry = (dynamic)null; 
            var highestBuyEntry = (dynamic)null;

            //if we don't have any open orders, we need to set the initial buys
            if (lowestOpenBuy != null && highestOpenBuy != null)
            {
                lowestBuyEntry = OrderEntries.Where(x => x.Id == Convert.ToInt32(lowestOpenBuy.Tag)).FirstOrDefault();
                highestBuyEntry = OrderEntries.Where(x => x.Id == Convert.ToInt32(highestOpenBuy.Tag)).FirstOrDefault();
            }
            else
            {
                lowestBuyEntry = OrderEntries.Where(x => x.Price >= bar.Price)
                    .OrderBy(x => x.Price)
                    .FirstOrDefault();
                highestBuyEntry = OrderEntries.Where(x => x.Price > bar.Price)
                    .OrderBy(x => x.Price)
                    .FirstOrDefault();
            }
            
            var nextLowestBuyEntry = OrderEntries.Where(x => x.Id == (lowestBuyEntry.Id - buySteps)).FirstOrDefault();
            var nextHighestBuyEntry = OrderEntries.Where(x => x.Id == (highestBuyEntry.Id + buySteps)).FirstOrDefault();
            var seedDepthEntry = OrderEntries.Where(x => x.Price >= (bar.Price * seedDepthPct))
                    .OrderBy(x => x.Price)
                    .FirstOrDefault();

            //place the buys
            while (nextLowestBuyEntry.Id >= seedDepthEntry.Id)
            {
                PlaceOrder(nextLowestBuyEntry.Price, nextLowestBuyEntry.Qty, nextLowestBuyEntry.Id.ToString());
                nextLowestBuyEntry = OrderEntries.Where(x => x.Id == (nextLowestBuyEntry.Id - buySteps)).FirstOrDefault();
            }
            
            while (nextHighestBuyEntry.Price < bar.Price)
            {
                PlaceOrder(nextHighestBuyEntry.Price, nextHighestBuyEntry.Qty, nextHighestBuyEntry.Id.ToString());
                nextHighestBuyEntry = OrderEntries.Where(x => x.Id == (nextHighestBuyEntry.Id + buySteps)).FirstOrDefault();
            }

        }


        /// <summary>
        /// This method sets up the order entries for reference
        /// </summary>
        private void SetupOrderEntries()
        {
            decimal p = 1;
            int id = 1;
            while (p < orderEntryMaxPrice)
            {
                OrderEntry oe = new OrderEntry();
                oe.Id = id;
                oe.Price = TruncateDecimal(p, 2);
                oe.Qty = TruncateDecimal((buySizeUSD / p), 6);
                OrderEntries.Add(oe);
                p = p * (1+stepPct);
                id++;
            }
        }

        private void PlaceOrder(decimal price, decimal qty, string tag)
        {
            string orderDirection;
            orderDirection = (qty > 0) ? "Buy" : "Sell";
            Debug(Time + " Placing " + orderDirection + " Order: " + price.ToString() + " " + qty.ToString() + " " + tag);
            var newTicket = LimitOrder(symbol, qty, price, tag);
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


