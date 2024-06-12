#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Custom.AddOns;
using NinjaTrader.Custom.AddOns.OrderFlowBot;
using NinjaTrader.Custom.AddOns.OrderFlowBot.BackTesting;
using NinjaTrader.Custom.AddOns.OrderFlowBot.DataBar;
using NinjaTrader.Custom.AddOns.OrderFlowBot.StrategiesIndicators;
using NinjaTrader.Custom.AddOns.OrderFlowBot.StrategiesIndicators.Strategies;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.Indicators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq; // Add this for LINQ methods
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
    public static class GroupConstants
    {
        public const string GROUP_NAME_STRATEGY = "Order Flow Bot";
        public const string GROUP_NAME_DATA_BAR = "Data Bar";
        public const string GROUP_NAME_INDICATORS = "Indicators";
        public const string GROUP_NAME_TESTING = "Testing";
    }

    [Gui.CategoryOrder(GroupConstants.GROUP_NAME_STRATEGY, 1)]
    [Gui.CategoryOrder(GroupConstants.GROUP_NAME_DATA_BAR, 2)]
    [Gui.CategoryOrder(GroupConstants.GROUP_NAME_INDICATORS, 3)]
    public partial class OrderFlowBot : Strategy
    {
		private ExhaustionPrintRatio _exhaustionPrintRatio;
		
        #region Variables

        private OrderFlowBotState _orderFlowBotState;
        private OrderFlowBotDataBars _dataBars;
        private StrategiesIndicatorsConfig _strategiesIndicatorsConfig;
        private StrategiesController _strategiesController;
        private OrderFlowBotPropertiesConfig _config;

        private OrderFlowBotJsonFile _jsonFile;

        private bool _entryLong;
        private bool _entryShort;
        private string _entryName;
        private List<string> _winningTradesExecutionIds;
        private List<string> _losingTradesExecutionIds;
        private string _atmStrategyId;
        private bool _isAtmStrategyCreated;
        // Prevent entry on same bar
        private int _lastTradeBarNumber;
		
		private TSSuperTrend TSSuperTrend1;

        #endregion

        #region Properties

        [NinjaScriptProperty]
        [Display(Name = "ATM Template Name", Description = "The ATM template name to use.", Order = 0, GroupName = GroupConstants.GROUP_NAME_STRATEGY)]
        public string AtmTemplateName { get; set; }

        #endregion

        #region Back Test Properties

        [NinjaScriptProperty]
        [Display(Name = "Back Testing Enabled", Description = "Enable this to back test all strategies and directions.", Order = 0, GroupName = GroupConstants.GROUP_NAME_TESTING)]
        public bool BackTestingEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "JSON File Enabled", Description = "Enable this to create a JSON file of trades to the desktop.", Order = 1, GroupName = GroupConstants.GROUP_NAME_TESTING)]
        public bool JsonFileEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Quantity", Description = "The name order quantity.", Order = 2, GroupName = GroupConstants.GROUP_NAME_TESTING)]
        public int Quantity { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Target", Description = "The target in ticks.", Order = 3, GroupName = GroupConstants.GROUP_NAME_TESTING)]
        public int Target { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Stop", Description = "The stop in ticks.", Order = 4, GroupName = GroupConstants.GROUP_NAME_TESTING)]
        public int Stop { get; set; }

        #endregion

        #region Indicators Properties

        [NinjaScriptProperty]
        [Display(Name = "Ratios Enabled", Description = "Enable ratios.", Order = 0, GroupName = GroupConstants.GROUP_NAME_INDICATORS)]
        public bool RatiosEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Last Ratios Price Enabled", Description = "Enable the last bid/ask ratios price.", Order = 1, GroupName = GroupConstants.GROUP_NAME_INDICATORS)]
        public bool LastRatiosPriceEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Single Print Enabled", Description = "Enable single print.", Order = 2, GroupName = GroupConstants.GROUP_NAME_INDICATORS)]
        public bool SinglePrintEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Single Print Bar Width", Description = "Adjust bar width for single print box.", Order = 3, GroupName = GroupConstants.GROUP_NAME_INDICATORS)]
        public double SinglePrintBarWidth { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Single Print Enabled", Description = "Adjust bar width shift for single print box.", Order = 4, GroupName = GroupConstants.GROUP_NAME_INDICATORS)]
        public double SinglePrintBarWidthShift { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Activate Stop Target Change", Description = "Activate the stop target change.", Order = 5, GroupName = GroupConstants.GROUP_NAME_STRATEGY)]
		public bool ActivateStopTargetChange { get; set; }
		
        #endregion

        #region DataBar Properties

        [NinjaScriptProperty]
        [Display(Name = "Look Back Bars", Description = "The maximum bars to look back.", Order = 0, GroupName = GroupConstants.GROUP_NAME_DATA_BAR)]
        public int LookBackBars { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Imbalance Ratio", Description = "The minimum imbalance ratio.", Order = 1, GroupName = GroupConstants.GROUP_NAME_DATA_BAR)]
        public double ImbalanceRatio { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Stacked Imbalance", Description = "The minimum number for a stacked imbalance.", Order = 2, GroupName = GroupConstants.GROUP_NAME_DATA_BAR)]
        public int StackedImbalance { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Valid Bid Volume", Description = "The valid bid volume.", Order = 3, GroupName = GroupConstants.GROUP_NAME_DATA_BAR)]
        public long ValidBidVolume { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Valid Ask Volume", Description = "The valid ask volume.", Order = 4, GroupName = GroupConstants.GROUP_NAME_DATA_BAR)]
        public long ValidAskVolume { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Valid Exhaustion Ratio", Description = "The valid exhaustion ratio for comparing top and bottom.", Order = 5, GroupName = GroupConstants.GROUP_NAME_DATA_BAR)]
        public double ValidExhaustionRatio { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Valid Absorption Ratio", Description = "The valid absorption ratio for comparing top and bottom.", Order = 6, GroupName = GroupConstants.GROUP_NAME_DATA_BAR)]
        public double ValidAbsorptionRatio { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Valid Volume Sequencing", Description = "The valid number of price to check for volume sequencing.", Order = 7, GroupName = GroupConstants.GROUP_NAME_DATA_BAR)]
        public int ValidVolumeSequencing { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Valid Volume Sequencing Minimum Volume", Description = "The valid number of volume to check for volume sequencing.", Order = 8, GroupName = GroupConstants.GROUP_NAME_DATA_BAR)]
        public long ValidVolumeSequencingMinimumVolume { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Ticks For SinglePrint", Description = "Ticks to add to SinglePrint", Order = 9, GroupName = GroupConstants.GROUP_NAME_DATA_BAR)]
        public int TicksBack { get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Time_start", Order=10, GroupName="Parameters")]
		public DateTime Time_start
		{ get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Time_end", Order=11, GroupName="Parameters")]
		public DateTime Time_end
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Time_breakstart", Order=12, GroupName="Parameters")]
		public DateTime Time_breakstart
		{ get; set; }
	
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Time_breakend", Order=13, GroupName="Parameters")]
		public DateTime Time_breakend
		{ get; set; }
		
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"A bot trading order flow";
                Name = "OrderFlowBot";
                Calculate = Calculate.OnEachTick;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;

                // Backtesting based on default settings
                Slippage = 2;
                IncludeCommission = true;
                Quantity = 1;
                Target = 16;
                Stop = 16;
                BackTestingEnabled = false;
                JsonFileEnabled = false;

                // OrderFlowBot
                AtmTemplateName = "OrderFlowBot";

                // DataBar
                LookBackBars = 4;
                ImbalanceRatio = 1.5;
                ValidBidVolume = 0;
                ValidAskVolume = 0;
                StackedImbalance = 3;
                ValidExhaustionRatio = 15;
                ValidAbsorptionRatio = 1.4;
                ValidVolumeSequencing = 4;
                ValidVolumeSequencingMinimumVolume = 500;
				TicksBack = 2;
				Time_start = DateTime.Parse("12:00", System.Globalization.CultureInfo.InvariantCulture);
				Time_end = DateTime.Parse("21:30", System.Globalization.CultureInfo.InvariantCulture);
				Time_breakstart = DateTime.Parse("18:00", System.Globalization.CultureInfo.InvariantCulture);
				Time_breakend = DateTime.Parse("19:00", System.Globalization.CultureInfo.InvariantCulture);
				
                // Indicators
                RatiosEnabled = true;
                LastRatiosPriceEnabled = true;
                SinglePrintEnabled = true;
                SinglePrintBarWidth = 1;
                SinglePrintBarWidthShift = 2;
				ActivateStopTargetChange = true;
            }
            else if (State == State.Configure)
            {
                _config = new OrderFlowBotPropertiesConfig
                {
                    TickSize = TickSize,
                    LookBackBars = LookBackBars,
                    ImbalanceRatio = ImbalanceRatio,
                    ValidBidVolume = ValidBidVolume,
                    ValidAskVolume = ValidAskVolume,
                    StackedImbalance = StackedImbalance,
                    ValidExhaustionRatio = ValidExhaustionRatio,
                    ValidAbsorptionRatio = ValidAbsorptionRatio,
                    ValidVolumeSequencing = ValidVolumeSequencing,
                    ValidVolumeSequencingMinimumVolume = ValidVolumeSequencingMinimumVolume,
                    SinglePrintBarWidth = SinglePrintBarWidth,
                    SinglePrintBarWidthShift = SinglePrintBarWidthShift
                };

                OrderFlowBotProperties.Initialize(_config);
            }
            else if (State == State.DataLoaded)
            {
                _dataBars = new OrderFlowBotDataBars();
                _orderFlowBotState = new OrderFlowBotState();
                _orderFlowBotState.BackTestingEnabled = BackTestingEnabled;
                _strategiesIndicatorsConfig = new StrategiesIndicatorsConfig();
                _strategiesController = new StrategiesController(_orderFlowBotState, _dataBars, _strategiesIndicatorsConfig);
				_exhaustionPrintRatio = new ExhaustionPrintRatio(_orderFlowBotState, _dataBars, "ExhaustionPrintRatio");
				
				TSSuperTrend1				= TSSuperTrend(Close, SuperTrendMode.ATR, 14, 2.618, MovingAverageType.HMA, 14, false, false, false);
				
                if (JsonFileEnabled)
                {
                    _jsonFile = new OrderFlowBotJsonFile();
                    _winningTradesExecutionIds = new List<string>();
                    _losingTradesExecutionIds = new List<string>();
                }

                ControlPanelSetStateDataLoaded();
                AddIndicators();

            }
            else if (State == State.Terminated)
            {
                ControlPanelSetStateTerminated();
            }
        }

		public TSSuperTrend GetTSSuperTrend()
{
    return TSSuperTrend1;
}

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                if (JsonFileEnabled)
                {
                    AppendWinningTradesToJsonFile();
                    AppendLosingTradesToJsonFile();
                }

                Reset();
            }
        }

        protected override void OnBarUpdate()
        {
            // Include all look back bars
            if (CurrentBar < LookBackBars)
                return;

            if (IsFirstTickOfBar)
            {
                // Get previous bar since we can miss the top or bottom of the bar in the data
                _dataBars.Bars.Add(GetDataBar(_dataBars.Bars, 1));
            }

            _dataBars.Bar = GetDataBar(_dataBars.Bars, 0);

            if (Position.MarketPosition == MarketPosition.Flat && BackTestingEnabled)
            {
                CheckStrategies();

                if (_entryLong)
                {
                    SetProfitTarget(_entryName, CalculationMode.Ticks, Target);
                    SetStopLoss(_entryName, CalculationMode.Ticks, Stop, false);
                    EnterLong(Quantity, _entryName);

                    _lastTradeBarNumber = _dataBars.Bar.BarNumber;
                }

                if (_entryShort)
                {
                    SetProfitTarget(_entryName, CalculationMode.Ticks, Target);
                    SetStopLoss(_entryName, CalculationMode.Ticks, Stop, false);
                    EnterShort(Quantity, _entryName);

                    _lastTradeBarNumber = _dataBars.Bar.BarNumber;
                }

                return;
            }

            CheckAtmStrategies();

        }

        private void AddIndicators()
        {
            if (RatiosEnabled)
            {
                Ratios ratios = Ratios();
                ratios.InitializeWith(_dataBars);
                AddChartIndicator(ratios);
            }

            if (LastRatiosPriceEnabled)
            {
                RatiosLastExhaustionAbsorptionPrice ratiosLastExhaustionAbsorptionPrice = RatiosLastExhaustionAbsorptionPrice();
                ratiosLastExhaustionAbsorptionPrice.InitializeWith(_dataBars);
                AddChartIndicator(ratiosLastExhaustionAbsorptionPrice);
            }

            if (SinglePrintEnabled)
            {
                SinglePrint singlePrint = SinglePrint();
                singlePrint.InitializeWith(_dataBars, _config);
                AddChartIndicator(singlePrint);
            }
        }

        private void AppendWinningTradesToJsonFile()
        {
            if (SystemPerformance.AllTrades.WinningTrades.Count > 0)
            {
                Trade lastTrade = SystemPerformance.AllTrades.WinningTrades[SystemPerformance.AllTrades.WinningTrades.Count - 1];
                double pnl = lastTrade.ProfitCurrency;

                if (!_winningTradesExecutionIds.Contains(lastTrade.Entry.ExecutionId))
                {
                    _winningTradesExecutionIds.Add(lastTrade.Entry.ExecutionId);
                    _jsonFile.Append(_dataBars, _lastTradeBarNumber, pnl, lastTrade.Entry.MarketPosition.ToString());
                }
            }
        }

        private void AppendLosingTradesToJsonFile()
        {
            if (SystemPerformance.AllTrades.LosingTrades.Count > 0)
            {
                Trade lastTrade = SystemPerformance.AllTrades.LosingTrades[SystemPerformance.AllTrades.LosingTrades.Count - 1];
                double pnl = lastTrade.ProfitCurrency;

                if (!_losingTradesExecutionIds.Contains(lastTrade.Entry.ExecutionId))
                {
                    _losingTradesExecutionIds.Add(lastTrade.Entry.ExecutionId);
                    _jsonFile.Append(_dataBars, _lastTradeBarNumber, pnl, lastTrade.Entry.MarketPosition.ToString());
                }
            }
        }

        private void Reset()
        {
            PrintOutput(String.Format("Exit | {0}", _entryName));

            _entryLong = false;
            _entryShort = false;
            _entryName = "";

            // Prevent re-entry on previous exit bar
            _lastTradeBarNumber = _dataBars.Bar.BarNumber + 1;

            _strategiesController.ResetStrategies();
            _orderFlowBotState.ValidStrategyDirection = Direction.Flat;
        }

        private bool AllowCheckStrategies()
        {
            if (_orderFlowBotState.SelectedTradeDirection == Direction.Flat || _dataBars.Bar.BarNumber <= _lastTradeBarNumber)
            {
                return false;
            }

            return true;
        }

        private void CheckStrategies()
        {
            if (!AllowCheckStrategies())
            {
                return;
            }

            _strategiesController.CheckStrategies();

            if (_orderFlowBotState.ValidStrategyDirection == Direction.Long && (TSSuperTrend1.UpTrend[0] > 0) && (Time_start.TimeOfDay <= Times[0][0].TimeOfDay && Times[0][0].TimeOfDay <= Time_end.TimeOfDay) &&
    !(Time_breakstart.TimeOfDay <= Times[0][0].TimeOfDay && Times[0][0].TimeOfDay <= Time_breakend.TimeOfDay))
            {
                _entryLong = true;
                _entryName = _orderFlowBotState.ValidStrategy.ToString();

                PrintOutput(String.Format("Enter Long | {0}", _entryName));


                //PrintDataBar(_dataBars.Bar);

                return;
            }

            if (_orderFlowBotState.ValidStrategyDirection == Direction.Short && (TSSuperTrend1.DownTrend[0] > 0) && (Time_start.TimeOfDay <= Times[0][0].TimeOfDay && Times[0][0].TimeOfDay <= Time_end.TimeOfDay) &&
    !(Time_breakstart.TimeOfDay <= Times[0][0].TimeOfDay && Times[0][0].TimeOfDay <= Time_breakend.TimeOfDay))
            {
                _entryShort = true;
                _entryName = _orderFlowBotState.ValidStrategy.ToString();

                PrintOutput(String.Format("Enter Short | {0}", _entryName));


                //PrintDataBar(_dataBars.Bar);

                return;
            }
        }

        private void CheckAtmStrategies()
        {
            if (State < State.Realtime || BackTestingEnabled)
            {
                return;
            }

            if (_isAtmStrategyCreated)
            {
                // Position was created and exited
                if (AtmPosition() == MarketPosition.Flat && (_orderFlowBotState.ValidStrategyDirection == Direction.Long ||
                    _orderFlowBotState.ValidStrategyDirection == Direction.Short))
                {
                    Reset();
                    ControlPanelOnExecutionUpdate();
                }
            }

            if (AtmPosition() == MarketPosition.Flat)
            {
                if (!AllowCheckStrategies())
                {
                    return;
                }

                _strategiesController.CheckStrategies();
																				
                if (_orderFlowBotState.ValidStrategyDirection == Direction.Long && (Time_start.TimeOfDay <= Times[0][0].TimeOfDay && Times[0][0].TimeOfDay <= Time_end.TimeOfDay) &&
    !(Time_breakstart.TimeOfDay <= Times[0][0].TimeOfDay && Times[0][0].TimeOfDay <= Time_breakend.TimeOfDay) ) 
                {
                    _atmStrategyId = GetAtmStrategyUniqueId();
                    _lastTradeBarNumber = _dataBars.Bar.BarNumber;
                    _entryName = _orderFlowBotState.ValidStrategy.ToString();

                    PrintOutput(String.Format("Enter Long | {0}", _entryName));
                    AtmStrategyCreate(OrderAction.Buy, OrderType.Market, 0, 0, TimeInForce.Day, _atmStrategyId, AtmTemplateName, _atmStrategyId, (atmCallbackErrorCode, atmCallbackId) =>
                    {	
                        if (atmCallbackId == _atmStrategyId)
                        {
                            if (atmCallbackErrorCode == ErrorCode.NoError)
                            {
                                _isAtmStrategyCreated = true;
                            }
                        }
                    });
    if (ActivateStopTargetChange)
    {
		double takeProfitPrice =_dataBars.Bar.Prices.Open + ( _dataBars.Bar.Prices.Open - _dataBars.Bar.Ratios.LastValidBidRatioPrice);
		AtmStrategyChangeStopTarget(takeProfitPrice, (_dataBars.Bar.Ratios.LastValidBidRatioPrice - TicksBack * _config.TickSize), "Stop1", _atmStrategyId);
        Print(_dataBars.Bar.Ratios.LastValidBidRatioPrice);
		Print(_dataBars.Bar.Prices.Open);
		Print(takeProfitPrice);
		
    }
                }

                if (_orderFlowBotState.ValidStrategyDirection == Direction.Short && (Time_start.TimeOfDay <= Times[0][0].TimeOfDay && Times[0][0].TimeOfDay <= Time_end.TimeOfDay) &&
    !(Time_breakstart.TimeOfDay <= Times[0][0].TimeOfDay && Times[0][0].TimeOfDay <= Time_breakend.TimeOfDay))
                {
                    _atmStrategyId = GetAtmStrategyUniqueId();
                    _lastTradeBarNumber = _dataBars.Bar.BarNumber;
                    _entryName = _orderFlowBotState.ValidStrategy.ToString();

                    PrintOutput(String.Format("Enter Short | {0}", _entryName));

                    AtmStrategyCreate(OrderAction.Sell, OrderType.Market, 0, 0, TimeInForce.Day, _atmStrategyId, AtmTemplateName, _atmStrategyId, (atmCallbackErrorCode, atmCallbackId) =>
                    {
                        if (atmCallbackId == _atmStrategyId)
                        {
                            if (atmCallbackErrorCode == ErrorCode.NoError)
                            {
                                _isAtmStrategyCreated = true;
                            }
                        }
                    });
					
    if (ActivateStopTargetChange)
    {
		double takeProfitPrice =_dataBars.Bar.Prices.Open - ( _dataBars.Bar.Ratios.LastValidAskRatioPrice-_dataBars.Bar.Prices.Open);
        AtmStrategyChangeStopTarget(takeProfitPrice, (_dataBars.Bar.Ratios.LastValidAskRatioPrice + TicksBack * _config.TickSize), "Stop1", _atmStrategyId);
		AtmStrategyChangeStopTarget(takeProfitPrice, 0, "Stop1", _atmStrategyId);
		Print(_dataBars.Bar.Ratios.LastValidAskRatioPrice);
		Print(_dataBars.Bar.Prices.Open);
		Print(takeProfitPrice);
    }
                }
            }
        }

        private MarketPosition AtmPosition()
        {
            if (_atmStrategyId == null)
            {
                return MarketPosition.Flat;
            }

            return GetAtmStrategyMarketPosition(_atmStrategyId);
        }

        private void CloseAtmPosition()
        {
            if (_atmStrategyId != null)
            {
                AtmStrategyClose(_atmStrategyId);
            }
        }
    }
}
