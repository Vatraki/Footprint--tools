using System.Linq;
using NinjaTrader.Custom.AddOns.OrderFlowBot.DataBar;
using NinjaTrader.NinjaScript.AddOns.OrderFlowBot;

namespace NinjaTrader.Custom.AddOns.OrderFlowBot.StrategiesIndicators.Strategies
	
{
    public class ExhaustionPrintRatio : StrategyBase
    {
        public override string Name { get; set; }
        public override Direction ValidStrategyDirection { get; set; }
	    private OrderFlowBotDataBar _previousDataBar;
		private int _lastBarIndex;
		
		
        public ExhaustionPrintRatio(OrderFlowBotState orderFlowBotState, OrderFlowBotDataBars dataBars, string name)
        : base(orderFlowBotState, dataBars, name)
        {
            // This can be used to initialize other values.
			 _previousDataBar = new OrderFlowBotDataBar();
			_lastBarIndex = -1;
        }
		       

        public override void CheckStrategy()
        {
            // Check if we are on a new bar
			
            if (IsNewBar())
            {
                _previousDataBar = dataBars.Bars.Last();

                if (IsValidLongDirection() && ValidStrategyDirection == Direction.Flat)
                {
                    CheckLong();
                }

                if (IsValidShortDirection() && ValidStrategyDirection == Direction.Flat)
                {
                    CheckShort();
                }
            }
        }
		
        public bool IsNewBar()
        {
            // Assuming dataBars.Bars.Count gives the total number of bars
            int currentBarIndex = dataBars.Bars.Count - 1;
            if (currentBarIndex != _lastBarIndex)
            {
                _lastBarIndex = currentBarIndex;
                return true;
            }
            return false;
        }

        // Bar is bullish and has x ask stacked imbalances.

        public override void CheckLong()
        {
            if (IsPreviousBarBullish() && (HasValidBidExhaustionRatio() || HasValidBidAbsorptionRatio()) && BidSinglePrint())
            {
                ValidStrategyDirection = Direction.Long;
            }
        }

        public override void CheckShort()
        {
            if (IsPreviousBarBearish() && (HasValidAskExhaustionRatio() || HasValidAskAbsorptionRatio()) && AskSinglePrint())
            {
                ValidStrategyDirection = Direction.Short;
            }
        }

        private bool IsBearishBar()
        {
            return dataBars.Bar.BarType == BarType.Bearish;
        }

        private bool HasValidAskExhaustionRatio()
        {
            return _previousDataBar.Ratios.HasValidAskExhaustionRatio;
        }

        private bool HasValidBidExhaustionRatio()
        {
            return _previousDataBar.Ratios.HasValidBidExhaustionRatio;
        }

        private bool HasValidAskAbsorptionRatio()
        {
            return _previousDataBar.Ratios.HasValidAskAbsorptionRatio;
        }

        private bool HasValidBidAbsorptionRatio()
        {
            return _previousDataBar.Ratios.HasValidBidAbsorptionRatio;
        }

        private bool AskSinglePrint()
        {
            return _previousDataBar.Volumes.HasAskSinglePrint;
        }

        private bool BidSinglePrint()
        {
            return _previousDataBar.Volumes.HasBidSinglePrint;
        }
		
		private bool IsPreviousBarBullish()
        {
            if (_previousDataBar != null)
            {
                return _previousDataBar.BarType == BarType.Bullish;
            }
            return false;
        }

		private bool IsPreviousBarBearish()
		{
		    if (_previousDataBar != null)
		    {
		        return _previousDataBar.BarType == BarType.Bearish;
		    }
		    return false;
		}

    }
}