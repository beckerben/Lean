using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.Becker
{
    public class Quant01 : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _eth = QuantConnect.Symbol.Create("ETHUSD", SecurityType.Crypto, Market.GDAX);

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 4, 4);  //Set Start Date
            SetEndDate(2018, 4, 5);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            AddCrypto("ETHUSD", Resolution.Minute);
            var threeMinutes = new TradeBarConsolidator(TimeSpan.FromMinutes(3));
            threeMinutes.DataConsolidated += OnThreeMinutes;
            SubscriptionManager.AddConsolidator("ETHUSD", threeMinutes);

        }

        public void OnThreeMinutes(object sender, TradeBar bar)
        {
            //Debug(Time.ToString("u") + " " + bar);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                Debug(Time.ToString("u") + " " + data);
            }
        }

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
    }
}


