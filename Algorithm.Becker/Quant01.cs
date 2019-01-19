using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.Becker
{
    public class Quant01 : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _eth = QuantConnect.Symbol.Create("ETHUSD", SecurityType.Crypto, Market.GDAX);
        private static RelativeStrengthIndex _rsi;

        const double buySize = 50;
        const double rsiLow = 15.9;
        const double rsiLowReset = 25;
        const double rsiHigh = 84.9;
        const double rsiHighReset = 75;
        const int rsiPeriods = 10;
        const int consolidatorMinutes = 3;


        private bool buyCocked = false;
        private bool buyFired = false;
        private bool sellCocked = false;
        private bool sellFired = false;


        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 4, 4);  //Set Start Date
            SetEndDate(2018, 4, 5);    //Set End Date
            SetCash(10000);            //Set Strategy Cash
            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);

            AddCrypto("ETHUSD", Resolution.Minute);

            var consolidator = new TradeBarConsolidator(TimeSpan.FromMinutes(consolidatorMinutes));
            consolidator.DataConsolidated += OnConsolidated;
            SubscriptionManager.AddConsolidator("ETHUSD", consolidator);

            _rsi = new RelativeStrengthIndex(10, MovingAverageType.Simple);

        }

        public void OnConsolidated(object sender, TradeBar bar)
        {
            _rsi.Update(bar.EndTime, bar.Close);
            SetSignals(bar);
            if (buyCocked && !buyFired)
            {
                PlaceBuy(bar);
            }
            if (sellCocked && !sellFired)
            {
                PlaceSell(bar);
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            //if (!Portfolio.Invested)
            //{
                //Debug(Time.ToString("u") + " " + data.Bars[_eth].Close);
            //}
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

        #region "helpers"

        private void SetSignals(TradeBar bar)
        {
            if (_rsi.IsReady)
            {
                var rsiValue = _rsi;
                if (_rsi <= rsiLow)
                {
                    if (!buyCocked)
                    {
                        buyCocked = true;
                    }
                }
                else
                {
                    if (_rsi >= rsiLowReset && buyCocked)
                    {
                        buyCocked = false;
                        buyFired = false;
                    }
                }

                if (_rsi >= rsiHigh)
                {
                    if (!sellCocked)
                    {
                        sellCocked = true;
                    }
                }
                else
                {
                    if (_rsi <= rsiHighReset && sellCocked)
                    {
                        sellCocked = false;
                        sellFired = false;
                    }
                }
            }
        }

        private void PlaceBuy (TradeBar bar)
        {
            Debug("Trigger buy " + Time.ToString("u") + " " + bar);
            buyFired = true;
        }

        private void PlaceSell (TradeBar bar)
        {
            Debug("Trigger sell " + Time.ToString("u") + " " + bar);
            sellFired = true;
        }

        #endregion //helpers

    }
}


