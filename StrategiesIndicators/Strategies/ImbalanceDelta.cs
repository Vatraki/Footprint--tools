using System.Linq;
using NinjaTrader.Custom.AddOns.OrderFlowBot.DataBar;
using NinjaTrader.NinjaScript.AddOns.OrderFlowBot;
using System;

namespace NinjaTrader.Custom.AddOns.OrderFlowBot.StrategiesIndicators.Strategies
{
    public class ImbalanceDelta : StrategyBase
    {
        public override string Name { get; set; }
        public override Direction ValidStrategyDirection { get; set; }
		private OrderFlowBotDataBar _previousDataBar;
		private int _lastBarIndex;
		

        public ImbalanceDelta(OrderFlowBotState orderFlowBotState, OrderFlowBotDataBars dataBars, string name)
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
            if (IsPreviousBarBullish() && HasAskImbalance() && (!HasBidImbalance()) && IsBullishMinMaxDifference() && CumulativeDeltapositif() && VolumePositif())
            {
                ValidStrategyDirection = Direction.Long;
            }
        }

        // Bar is bearish and has x bid stacked imbalances.
        public override void CheckShort()
        {
            if (IsPreviousBarBearish() && HasBidImbalance() && (!HasAskImbalance()) && IsBearishMinMaxDifference() && CumulativeDeltanegatif() && VolumePositif())
            {
                ValidStrategyDirection = Direction.Short;
            }
        }

        private bool IsBullishBar()
        {
            return dataBars.Bar.BarType == BarType.Bullish;
        }

        private bool IsBearishBar()
        {
            return dataBars.Bar.BarType == BarType.Bearish;
        }

        private bool HasValidAskStackedImbalance()
        {
            return dataBars.Bar.Imbalances.HasAskStackedImbalances;
        }

        private bool HasValidBidStackedImbalance()
        {
            return dataBars.Bar.Imbalances.HasBidStackedImbalances;
        }
		private bool HasAskImbalance()
        {
            return _previousDataBar.Imbalances.HasAskImbalance();
        }

        private bool HasBidImbalance()
        {
            return _previousDataBar.Imbalances.HasBidImbalance();
			
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
		
        private bool IsBullishMinMaxDifference()
        {
            long maxDelta = Math.Abs(_previousDataBar.Deltas.MaxDelta);
            long minDelta = Math.Abs(_previousDataBar.Deltas.MinDelta);
            //bool validMinDelta = minDelta < 100;

            return maxDelta >= 2.5 * minDelta && _previousDataBar.Deltas.Delta > 150;
        }

        private bool IsBearishMinMaxDifference()
        {
            long maxDelta = Math.Abs(_previousDataBar.Deltas.MaxDelta);
            long minDelta = Math.Abs(_previousDataBar.Deltas.MinDelta);
            //bool validMaxDelta = maxDelta < 100;

            return minDelta >= 2.5 * maxDelta && _previousDataBar.Deltas.Delta < -150;
        }
		private bool CumulativeDeltapositif(){ 
			return _previousDataBar.Deltas.CumulativeDelta > 1000;}
		
		private bool CumulativeDeltanegatif(){ 
			return _previousDataBar.Deltas.CumulativeDelta < -1000;}
		
		private bool VolumePositif(){
//			return _previousDataBar.Volumes.Volume > 500 && _previousDataBar.Volumes.Volume < 200000 ;
			return true;
		}
    }
}
