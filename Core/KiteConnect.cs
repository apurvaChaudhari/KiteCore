using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using CsvHelper;
using KiteCore.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace KiteCore.Core
{
    public class KiteConnect
    {
        /// <summary>
        /// The default root entry
        /// </summary>
        private const string Root = "https://api.kite.trade";

        /// <summary>
        /// The login url
        /// </summary>
        private const string Login = "https://kite.trade/connect/login";

        private readonly string _apiKey;
        private readonly bool _microCache;


        /// <summary>
        /// A dictionary of API Routes that can be accessed
        /// </summary>
        private readonly Dictionary<string, string> _routes = new Dictionary<string, string>
        {
            ["parameters"] = "/parameters",
            ["api.validate"] = "/session/token",
            ["api.invalidate"] = "/session/token",
            ["user.margins"] = "/user/margins/{segment}",
            ["orders"] = "/orders",
            ["trades"] = "/trades",
            ["orders.info"] = "/orders/{order_id}",
            ["orders.place"] = "/orders/{variety}",
            ["orders.modify"] = "/orders/{variety}/{order_id}",
            ["orders.cancel"] = "/orders/{variety}/{order_id}",
            ["orders.trades"] = "/orders/{order_id}/trades",
            ["portfolio.positions"] = "/portfolio/positions",
            ["portfolio.holdings"] = "/portfolio/holdings",
            ["portfolio.positions.modify"] = "/portfolio/positions",
            ["market.instruments.all"] = "/instruments",
            ["market.instruments"] = "/instruments/{exchange}",
            ["market.quote"] = "/instruments/{exchange}/{tradingsymbol}",
            ["market.historical"] = "/instruments/historical/{instrument_token}/{interval}",
            ["market.trigger_range"] = "/instruments/{exchange}/{tradingsymbol}/trigger_range"
        };

        private string _accessToken;

        /// <summary>
        ///     Initialise a new Kite Connect client instance.
        ///     - `api_key` is the key issued to you
        ///     - `access_token` is the token obtained after the login flow in
        ///     exchange for the `request_token` . Pre - login, this will default to None,
        ///     but once you have obtained it, you should
        ///     persist it in a database or session to pass
        ///     to the Kite Connect class initialisation for subsequent requests.
        ///     - `root` is the API end point root.Unless you explicitly
        ///     want to send API requests to a non-default endpoint, this
        ///     can be ignored.
        ///     - `debug`, if set to True, will serialise and print requests
        ///     and responses to stdout.
        ///     - `timeout` is the time (seconds) for which the API client will wait for
        ///     a request to complete before it fails.
        ///     - `micro_cache`, when set to True, will fetch the last cached
        ///     version of an API response if available.This saves time on
        ///     a roundtrip to the backend.Micro caches only live for several
        ///     seconds to prevent data from turning stale.
        /// </summary>
        /// <param name="apiKey">Your Api key</param>
        /// <param name="timeout">This parametere is unused and is maintained just to keep consistancy in the function call.The timeout of th library is used</param>
        /// <param name="accessToken">Your access token that u get after login</param>
        /// <param name="debug">This parameter is unused and is maintained to introduce debug possibilites in the future</param>
        /// <param name="microCache">to enable or disable microCache </param>
        public KiteConnect(string apiKey, int timeout = 100, string accessToken = null, bool
            debug = false, bool microCache = true) //this.session_hook
        {
            this._apiKey = apiKey;
            this._accessToken = accessToken;
            this._microCache = microCache;
            //this.session_hook = None
        }

        // orders
        /// <summary>
        ///     https://kite.trade/docs/connect/v1/?shell#placing-orders
        ///     Placing an order means registering it with the OMS via the API. This does not guarantee the order reaching the
        ///     exchange. The fate of the order
        ///     is dependent on many factors including market hours, margin requirements, risk checks, and so on.Under normal
        ///     circumstances,
        ///     everything from placement to confirmation of order receipt by the OMS happens in several hundred milliseconds.
        ///     When an order is successfully placed, the API returns an order_id.The status of
        ///     the order is not known at the moment of
        ///     placing for the aforementioned reasons.
        ///     Moreover, in case of non-MARKET orders that may be open for an indefinite time,
        ///     it is not practical to poll the orders API continuously to know the status. For this,
        ///     you should setup an HTTP postback endpoint where Kite Connect will asynchronously post
        ///     updates to orders as they happen.
        ///     curl https://api.kite.trade/orders/regular \
        ///     -d "api_key=xxx" \
        ///     -d "access_token=yyy" \
        ///     -d "tradingsymbol=ACC" \
        ///     -d "exchange=NSE" \
        ///     -d "transaction_type=BUY" \
        ///     -d "order_type=MARKET" \
        ///     -d "quantity=1" \
        ///     -d "product=MIS" \
        ///     -d "validity=DAY"
        /// </summary>
        /// <param name="exchange">Type of exchange "MCXSXFO","MCXSXCM","BSE","NSE","BFO","NFO","NCDEX","MCX","CDS","MCXSX"</param>
        /// <param name="tradingSymbol">The trading name of script "ACC" </param>
        /// <param name="transactionType"> "BUY","SELL"</param>
        /// <param name="quantity">Quantity to buy e.g "1","10"</param>
        /// <param name="price">Price at which to buy or sell e.g "100.05"</param>
        /// <param name="product">"BO","CO","CNC", "MIS", "NRML"</param>
        /// <param name="orderType">"LIMIT","MARKET","SL","SL-M"</param>
        /// <param name="validity">"DAY" "IOC"</param>
        /// <param name="disclosedQuantity">Quantity to disclose publicly (for equity trades)</param>
        /// <param name="triggerPrice">For SL, SL-M etc.</param>
        /// <param name="squareoffValue">
        /// Bracket Order (BO) parameters
        /// squareoff_value	Price difference at which the order should be squared off and profit booked (eg: Order price is 100. Profit target is 102. So squareoff_value = 2)
        /// </param>
        /// <param name="stoplossValue">
        /// Bracket Order (BO) parameters
        /// stoploss_value	Stoploss difference at which the order should be squared off (eg: Order price is 100. Stoploss target is 98. So stoploss_value = 2)
        /// </param>
        /// <param name="trailingStoploss">
        /// Bracket Order (BO)/Cover order (CO) parameters
        /// trailing_stoploss	Incremental value by which stoploss price changes when market moves in your favor by the same incremental value from the entry price (optional)
        /// </param>
        /// <param name="variety">Order variety (regular, amo etc.)</param>
        /// <returns>The order id of placed order in string format</returns>
        public string OrderPlace(
            string exchange,
            string tradingSymbol,
            string transactionType,
            string quantity,
            string orderType,
            string product,
            string validity = null,
            string price = null,
            string disclosedQuantity = null,
            string triggerPrice = null,
            string squareoffValue = null,
            string stoplossValue = null,
            string trailingStoploss = null,
            string variety = "regular")
        {
            //Place an order.
            var param = new Dictionary<string, string>();
            if (exchange != null)
                param.Add("exchange", exchange);
            if (tradingSymbol != null)
                param.Add("tradingsymbol", tradingSymbol);
            if (transactionType != null)
                param.Add("transaction_type", transactionType);
            if (quantity != null)
                param.Add("quantity", quantity);
            if (price != null)
                param.Add("price", price);
            if (product != null)
                param.Add("product", product);
            if (orderType != null)
                param.Add("order_type", orderType);
            if (validity != null)
                param.Add("validity", validity);
            if (disclosedQuantity != null)
                param.Add("disclosed_quantity", disclosedQuantity);
            if (triggerPrice != null)
                param.Add("trigger_price", triggerPrice);
            if (squareoffValue != null)
                param.Add("squareoff_value", squareoffValue);
            if (stoplossValue != null)
                param.Add("stoploss_value", stoplossValue);
            if (trailingStoploss != null)
                param.Add("trailing_stoploss", trailingStoploss);
            if (variety != null)
                param.Add("variety", variety);

            return Post("orders.place", param)["order_id"];
        }

        /// <summary>
        ///     https://kite.trade/docs/connect/v1/?shell#modifying-orders
        /// </summary>
        /// <param name="orderId">The order ID receieved when placing the order</param>
        /// <param name="parentOrderId">
        /// Bracket Order (BO) parameters
        /// parent_order_id	Id of the parent order (obtained from the /orders call) as BO is a multi-legged order
        /// </param>
        /// <param name="exchange">Type of exchange "MCXSXFO","MCXSXCM","BSE","NSE","BFO","NFO","NCDEX","MCX","CDS","MCXSX" </param>
        /// <param name="tradingSymbol">The trading name of script "ACC" </param>
        /// <param name="transactionType"> "BUY","SELL"</param>
        /// <param name="quantity">Quantity to buy e.g "1","10"</param>
        /// <param name="price">Price at which to modify buy or sell e.g "100.05"</param>
        /// <param name="orderType">"LIMIT","MARKET","SL","SL-M"</param>
        /// <param name="product">"BO","CO","CNC", "MIS", "NRML"</param>
        /// <param name="triggerPrice">For SL, SL-M etc.(enter here to modify)</param>
        /// <param name="validity">"DAY" "IOC" </param>
        /// <param name="disclosedQuantity">Quantity to disclose publicly (for equity trades)</param>
        /// <param name="variety">Order variety (regular, amo etc.)</param>
        /// <returns>The order id of placed order in string format</returns>
        public string OrderModify(
            string orderId,
            string parentOrderId = null,
            string exchange = null,
            string tradingSymbol = null,
            string transactionType = null,
            string quantity = null,
            string price = null,
            string orderType = null,
            string product = null,
            string triggerPrice = null,
            string validity = "DAY",
            string disclosedQuantity = null,
            string variety = "regular")
        {
            //Modify an open order.
            var param = new Dictionary<string, string>();
            var param2 = new Dictionary<string, string>();
            var param3 = new Dictionary<string, string>();
            if (orderId != null)
                param.Add("order_id", orderId);
            if (parentOrderId != null)
                param.Add("parent_order_id", parentOrderId);
            if (exchange != null)
                param.Add("exchange", exchange);
            if (tradingSymbol != null)
                param.Add("tradingsymbol", tradingSymbol);
            if (transactionType != null)
                param.Add("transaction_type", transactionType);
            if (quantity != null)
                param.Add("quantity", quantity);
            if (price != null)
                param.Add("price", price);
            if (orderType != null)
                param.Add("order_type", orderType);
            if (product != null)
                param.Add("product", product);
            if (triggerPrice != null)
                param.Add("trigger_price", triggerPrice);
            if (validity != null)
                param.Add("validity", validity);
            if (disclosedQuantity != null)
                param.Add("disclosed_quantity", disclosedQuantity);
            if (variety != null)
                param.Add("variety", variety);

            if (variety == "BO")
            {
                param2.Add("order_id", orderId);
                param2.Add("quantity", quantity);
                param2.Add("price", price);
                param2.Add("trigger_price", triggerPrice);
                param2.Add("disclosed_quantity", disclosedQuantity);
                param2.Add("variety", variety);
                param2.Add("parent_order_id", parentOrderId);
                return Put("order_modify", param2)["order_id"];
            }
            if (variety == "CO")
            {
                param3.Add("order_id", orderId);
                param3.Add("trigger_price", triggerPrice);
                param3.Add("variety", variety);
                param3.Add("parent_order_id", parentOrderId);
                return Put("order_modify", param3)["order_id"];
            }
            return Put("orders.modify", param)["order_id"];
        }

        /// <summary>
        ///     https://kite.trade/docs/connect/v1/?shell#cancelling-orders
        /// </summary>
        /// <param name="orderId">The order ID receieved when placing the order</param>
        /// <param name="variety">Order variety (regular, amo etc.)</param>
        /// <param name="parentOrderId">
        /// Bracket Order (BO) parameters
        /// parent_order_id	Id of the parent order (obtained from the /orders call) as BO is a multi-legged order
        /// </param>
        /// <returns>The order id of placed order in string format</returns>
        public string OrderCancel(string orderId, string variety = "regular", string parentOrderId = null)
        {
            //Cancel an order
            var param = new Dictionary<string, string>
            {
                {"order_id", orderId},
                {"variety", variety}
            };
            if (parentOrderId != null)
            {
                param.Add("parent_order_id", parentOrderId);
            }
            return Delete("orders.cancel", param)["order_id"];
        }

        /// <summary>
        ///     https://kite.trade/docs/connect/v1/?shell#retrieving-orders
        ///     https://kite.trade/docs/connect/v1/?shell#retrieving-an-individual-order
        /// </summary>
        /// <param name="orderId">The order ID receieved when placing the order</param>
        /// <returns>
        /// A list of dictionarys in following format
        /// [{
        ///"status": "REJECTED",
        ///"product": "NRML",
        /// "pending_quantity": 0,
        ///  "order_type": "MARKET",
        ///  "exchange": "NFO",
        ///  "order_id": "151220000000000",
        ///  "parent_order_id": "151210000000000",
        ///  "price": 0.0,
        ///  "exchange_order_id": null,
        ///  "order_timestamp": "2015-12-20 15:01:43",
        ///  "transaction_type": "BUY",
        ///  "trigger_price": 0.0,
        ///  "validity": "DAY",
        /// "disclosed_quantity": 0,
        ///  "status_message": "RMS:Margin Exceeds, Required:0, Available:0",
        ///  "average_price": 0.0,
        ///  "quantity": 75
        ///}, {
        ///  "status": "VALIDATION PENDING",
        ///  "product": "NRML",
        /// "pending_quantity": 75,
        ///  "order_type": "MARKET",
        ///  "exchange": "NFO",
        ///  "order_id": "151220000000000",
        ///  "parent_order_id": "151210000000000",
        ///  "price": 0.0,
        ///  "exchange_order_id": null,
        ///  "order_timestamp": "2015-12-20 15:01:43",
        ///  "transaction_type": "BUY",
        ///  "trigger_price": 0.0,
        ///  "validity": "DAY",
        ///  "disclosed_quantity": 0,
        ///  "status_message": null,
        ///  "average_price": 0.0,
        ///  "quantity": 75
        ///}, {
        ///  "status": "PUT ORDER REQ RECEIVED",
        ///  "product": "NRML",
        ///  "pending_quantity": 0,
        ///  "order_type": "MARKET",
        ///  "exchange": "NFO",
        ///  "order_id": "151220000000000",
        ///  "parent_order_id": "151210000000000",
        ///  "price": 0.0,
        ///  "exchange_order_id": null,
        ///  "order_timestamp": "2015-12-20 15:01:43",
        ///  "transaction_type": "BUY",
        ///  "trigger_price": 0.0,
        ///  "validity": "DAY",
        /// "disclosed_quantity": 0,
        ///  "status_message": null,
        ///  "average_price": 0.0,
        ///  "quantity": 75
        ///}]</returns>
        public dynamic Orders(string orderId = null)
        {
            var param = new Dictionary<string, string> {{"order_id", orderId}};
            if (orderId != null)
            {
                return Get("orders.info", param);
            }
            return Get("orders", param);
        }

        /// <summary>
        ///     https://kite.trade/docs/connect/v1/?shell#retrieving-trades
        /// </summary>
        /// <param name="orderId">The order ID receieved when placing the order
        /// If null all orders in day will be retrieved
        /// </param>
        /// <returns>
        ///  A list of dictionarys in following format
        ///    [{
        ///    "trade_id": 159918,
        ///            "order_id": "151220000000000",
        ///            "exchange_order_id": "511220371736111",
        ///    "tradingsymbol": "ACC",
        ///            "exchange": "NSE",
        ///            "instrument_token": "22",
        ///    "transaction_type": "BUY",
        ///            "product": "MIS",
        ///            "average_price": 100.98,
        ///            "quantity": 10,
        ///    "order_timestamp": "2015-12-20 15:01:44",
        ///            "exchange_timestamp": "2015-12-20 15:01:43"
        ///}]
        /// </returns>
        public dynamic Trades(string orderId = null)
        {
            //      Retreive the list of trades executed(all or ones under a particular order).
            //An order can be executed in tranches based on market conditions.
            //      These trades are individually recorded under an order.
            //      - `order_id` is the ID of the order (optional)whose trades are to be retrieved.
            //      If no `order_id` is specified, all trades for the day are returned.
            var param = new Dictionary<string, string> {{"order_id", orderId}};
            if (orderId != null)
            {
                return Get("orders.trades", param);
            }
            return Get("trades");
        }

        /// <summary>
        ///     https://kite.trade/docs/connect/v1/?shell#positions
        /// </summary>
        /// <param name="type"> "net" or "day"</param>
        /// <returns>
        /// A dictionary in following format 
        ///    {
        ///"net": [{
        ///  "tradingsymbol": "NIFTY15DEC9500CE",
        ///  "exchange": "NFO",
        ///  "instrument_token": 41453,
        ///  "product": "NRML",
        ///  "quantity": -100,
        /// "overnight_quantity": -100,
        ///"multiplier": 1,
        ///  "average_price": 3.475,
        ///  "close_price": 0.75,
        ///  "last_price": 0.75,
        ///  "net_value": 75.0,
        ///  "pnl": 272.5,
        ///  "m2m": 0.0,
        ///  "unrealised": 0.0,
        ///  "realised": 0.0,
        ///  "buy_quantity": 0,
        ///  "buy_price": 0,
        ///  "buy_value": 0.0,
        ///  "buy_m2m": 0.0,
        ///  "sell_quantity": 100,
        ///  "sell_price": 3.475,
        ///  "sell_value": 347.5,
        ///  "sell_m2m": 75.0
        ///}],
        ///OR
        ///"day": []
        ///}
        ///  </returns>
        public Dictionary<string, string> Positions(string type)
        {
            //Retrieve the list of positions.
            dynamic temp = Get("portfolio.positions");
            return ConrvToStr(temp[type]);
        }

        /// <summary>
        ///     https://kite.trade/docs/connect/v1/?shell#holdings
        /// </summary>
        /// <returns>
        /// A list of dictionaries in following format
        ///[{
        ///  "tradingsymbol": "ABHICAP",
        ///  "exchange": "BSE",
        ///  "isin": "INE516F01016",
        ///  "quantity": 0,
        ///  "realised_quantity": 1,
        ///  "t1_quantity": 1,
        ///  "average_price": 94.75,
        ///  "last_price": 93.75,
        ///  "pnl": -100.0,
        ///  "product": "CNC",
        ///  "collateral_quantity": 0,
        ///  "collateral_type": null
        ///}, {
        ///  "tradingsymbol": "AXISBANK",
        ///  "exchange": "NSE",
        ///  "isin": "INE238A01034",
        ///  "quantity": 1,
        ///  "realised_quantity": 1,
        ///  "t1_quantity": 0,
        ///  "average_price": 475.0,
        ///  "last_price": 432.55,
        ///  "pnl": -42.50,
        ///  "product": "CNC",
        ///  "collateral_quantity": 0,
        ///  "collateral_type": null
        ///}]
        /// </returns>
        public dynamic Holdings()
        {
            return Get("portfolio.holdings");
        }


        /// <summary>
        ///not used this i dont know what it does this function is portd form the python lib i have kept it so as to maintain completeness
        /// </summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="tradingSymbol">The trading symbol.</param>
        /// <param name="transactionType">Type of the transaction.</param>
        /// <param name="positionType">Type of the position.</param>
        /// <param name="quantity">The quantity.</param>
        /// <param name="oldProduct">The old product.</param>
        /// <param name="newProduct">The new product.</param>
        /// <returns></returns>
        public Dictionary<string, string> ProductModify(
            string exchange,
            string tradingSymbol,
            string transactionType,
            string positionType,
            string quantity,
            string oldProduct,
            string newProduct)
        {
            //Modify an open position's product type.
            var param = new Dictionary<string, string>
            {
                ["exchange"] = exchange,
                ["tradingsymbol"] = tradingSymbol,
                ["transaction_type"] = transactionType,
                ["position_type"] = positionType,
                ["quantity"] = quantity,
                ["old_product"] = oldProduct,
                ["new_product"] = newProduct
            };
            return Put("portfolio.positions.modify", param);
        }

        /// <summary>
        ///     https://kite.trade/docs/connect/v1/?shell#retrieving-full-instrument-list
        /// Retrieve the list of market instruments available to trade
        /// </summary>
        /// <param name="exchange">Type of exchange "BSE" or "NSE"</param>
        /// <returns>list of dictionsries of market instruments</returns>
        public List<Dictionary<string, string>> Instruments(string exchange = null)
        {
            //    def instruments(self, exchange= None):
            //Retrieve the list of market instruments available to trade.
            //      Note that the results could be large, several hundred KBs in size,
            //with tens of thousands of entries in the list.

            if (exchange != null)
            {
                var param = new Dictionary<string, string> {["exchange"] = exchange};

                return ParseCsv(Get("market.instruments", param));
            }
            return ParseCsv(Get("market.instruments.all"));
        }

        /// <summary>
        /// stores instruments in  csv file
        /// </summary>
        /// <param name="path">The path where to store file e.g
        /// Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "InstrumentList.csv")
        /// </param>
        /// <param name="exchange">Type of exchange "BSE" or "NSE" </param>
        public void StoreInstruments(string path, string exchange = null)
        {
            using (TextWriter writer = File.CreateText(path))
            {
                string data;
                if (exchange != null)
                {
                    var param = new Dictionary<string, string> {["exchange"] = exchange};
                    data = Get("market.instruments", param);
                }
                else
                {
                    data = Get("market.instruments.all");
                }
                writer.Write(data);
            }
        }

        /// <summary>
        ///     https://kite.trade/docs/connect/v1/?shell#retrieving-market-quotes
        /// </summary>
        /// <param name="insTocken">The instrument token e.g "4995329"</param>
        /// <param name="interval">the interval i.e 
        ///minute
        ///day
        ///3minute
        ///5minute
        ///10minute
        ///15minute
        ///30minute
        ///60minute
        /// </param>
        /// <param name="days">number of days back quote to retrive
        /// </param>
        /// <param name="noofdaybak">Use paremetere when using this function after a exchange holiday other wise a blank Jarray will be returned</param>
        /// <returns>A Jarray It is a list of lists access inner list using enum in data helpers 
        ///public enum Ohlc
        ///{
        ///  Time = 0,
        ///  Open = 1,
        ///  High,
        /// Low,
        /// Close
        ///}
        ///  </returns>
        public JArray Historical(string insTocken, string interval, int days, int noofdaybak = 0)
        {
            if (DateTime.Now.DayOfWeek == DayOfWeek.Monday)
            {
                days = days*-3;
            }
            else if (noofdaybak > 0)
            {
                days = days*-1*noofdaybak;
            }
            else
            {
                days = days*-1;
            }
            string startdate = DateTime.Now.ToString("yyyy-MM-dd");
            string enddate = DateTime.Now.AddDays(days).ToString("yyyy-MM-dd");
            //Retrieve quote and market depth for an instrument
            var param = new Dictionary<string, string>
            {
                {"from", enddate},
                {"to", startdate},
                {"instrument_token", insTocken},
                {"interval", interval}
            };
            return Get("market.historical", param)["candles"];
        }

        /// <summary>
        /// Retreives the quote of a tradingSymbol from particular exchange 
        /// </summary>
        /// <param name="exchange">Type of exchange "MCXSXFO","MCXSXCM","BSE","NSE","BFO","NFO","NCDEX","MCX","CDS","MCXSX" </param>
        /// <param name="tradingSymbol">The trading symbol eg "INFY"</param>
        /// <returns>
        /// A nested dictionary in the following format
        ///{
        ///   "last_price": 1083.15,
        ///   "change_percent": 0.0,
        ///   "change": 0.0,
        ///   "volume": 0,
        ///   "buy_quantity": 0,
        ///   "sell_quantity": 0,
        ///   "open_interest": 0,
        ///   "last_quantity": 5,
        ///   "last_time": "2015-12-15 10:16:36",
        ///   "ohlc": {
        ///     "open": 1103.4,
        ///     "high": 1103.4,
        ///     "low": 1079.45,
        ///     "close": 1083.15
        ///   },
        ///   "depth": {
        ///     "buy": [{
        ///       "price": 0,
        ///       "orders": 0,
        ///       "quantity": 0
        ///     }],
        ///     "sell": [{
        ///       "price": 0,
        ///       "orders": 0,
        ///       "quantity": 0
        ///     }]
        ///   }
        /// }
        /// </returns>
        public dynamic Quote(string exchange, string tradingSymbol)
        {
            //Retrieve quote and market depth for an instrument
            var param = new Dictionary<string, string>
            {
                {"exchange", exchange},
                {"tradingsymbol", tradingSymbol}
            };
            return Get("market.quote", param);
        }

        /// <summary>
        /// Retrieve the buy/sell trigger range for Cover Orders.
        /// </summary>
        /// <param name="exchange">Type of exchange "MCXSXFO","MCXSXCM","BSE","NSE","BFO","NFO","NCDEX","MCX","CDS","MCXSX"</param>
        /// <param name="tradingSymbol">The trading symbol The trading symbol eg "INFY"</param>
        /// <param name="transactionType">Type of the transaction "BUY","SELL"</param>
        /// <returns>
        /// A dictionary containing 3 values
        /// E.g for "BUY" transaction type
        /// start 100 -->the start of range
        /// percent 10 -->the percent range
        /// end 90  -->the end of range
        /// </returns>
        /// 
        public dynamic TriggerRange(string exchange, string tradingSymbol, string transactionType)
        {
            //Retrieve the buy/sell trigger range for Cover Orders.
            var param = new Dictionary<string, string>
            {
                {"exchange", exchange},
                {"tradingsymbol", tradingSymbol},
                {"transaction_type", transactionType}
            };
            return Get("market.trigger_range", param);
        }

        private List<Dictionary<string, string>> ParseCsv(string str)
        {
            var records = new List<Dictionary<string, string>>();
            TextReader textReader = new StringReader(str.Replace(" ", ""));
            var csv = new CsvParser(textReader);
            string[] header = csv.Read();
            while (true)
            {
                string[] row = csv.Read();
                if (row == null)
                {
                    break;
                }
                Dictionary<string, string> dic = header.Zip(row, (k, v) => new {k, v}).ToDictionary(x => x.k, x => x.v);
                records.Add(dic);
            }
            textReader.Close();
            return records;
        }

        private dynamic Get(string route, Dictionary<string, string> param = null)
        {
            //Alias for sending a GET request
            return Request(route, "GET", param);
        }

        private dynamic Post(string route, Dictionary<string, string> param = null)
        {
            //Alias for sending a POST request.
            return Request(route, "POST", param);
        }

        private dynamic Put(string route, Dictionary<string, string> param = null)
        {
            //Alias for sending a PUT request.
            return Request(route, "PUT", param);
        }

        private dynamic Delete(string route, Dictionary<string, string> param = null)
        {
            //Alias for sending a DELETE request.

            return Request(route, "DELETE", param);
        }

        private dynamic Request(string route, string method, Dictionary<string, string> param = null)
        {
            var client = new RestClient(Root);

            //Micro cache?
            if (this._microCache == false)
                if (param != null) param["no_micro_cache"] = "1";

            //Is there a token?.
            if ((this._accessToken != null) && (param != null))
                param["access_token"] = this._accessToken;

            if (param != null)
            {
                param["api_key"] = this._apiKey; //caution
            }
            string uri = this._routes[route];

            //setup of request and client
            //request.Timeout = timeout;
            client.FollowRedirects = true;

            if (uri.Contains("{")) //add url segments
            {
                if (param != null)
                    foreach (KeyValuePair<string, string> kvp in param)
                    {
                        if (uri.Contains(kvp.Key))
                        {
                            uri = uri.Replace("{" + kvp.Key + "}", kvp.Value);
                        }
                    }
            }

            var request = new RestRequest(uri);
            //if (param != null)
            //    foreach (KeyValuePair<string, string> kvp in param)
            //    {
            //        request.AddUrlSegment(kvp.Key, kvp.Value);
            //    }
            if (method == "POST")
            {
                request.Method = Method.POST;
            }
            else if (method == "GET")
            {
                request.Method = Method.GET;
            }
            else if (method == "PUT")
            {
                request.Method = Method.PUT;
            }
            else if (method == "DELETE")
            {
                request.Method = Method.DELETE;
            }
            if (param != null)
            {
                foreach (KeyValuePair<string, string> kvp in param)
                {
                    request.AddParameter(kvp.Key, kvp.Value);
                }
                request.AddJsonBody(param);
            }
            else
            {
                param = new Dictionary<string, string>
                {
                    ["access_token"] = this._accessToken,
                    ["api_key"] = this._apiKey
                };
                request.AddJsonBody(param);
            }

            IRestResponse response = client.Execute(request);
            string content = response.Content;

            if (response.ErrorException != null)
            {
                string message = response.ErrorMessage;
                var restExcept = new ApplicationException(message, response.ErrorException);
                throw restExcept;
            }

            if (response.ContentType.Contains("json"))
            {
                dynamic responseDict;
                try
                {
                    responseDict = GetDict(content);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("could not parse json", ex);
                }

                // api error
                if (responseDict["status"] == "error")
                {
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        if (responseDict["error_type"] == "TwoFAException")
                            throw new ApplicationException("kite api error TwoFAException"
                                                           + response.StatusDescription);
                        throw new ApplicationException("kite api error " + responseDict["error_type"] +
                                                       "status" + response.StatusDescription);
                    }
                }
                return responseDict["data"];
            }
            if (response.ContentType.Contains("csv"))
            {
                return content;
            }
            throw new ApplicationException();
        }

        private static string Sha256(string password)
        {
            var crypt = new SHA256Managed();
            var hash = new StringBuilder();
            if (password != null)
            {
                byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(password), 0,
                    Encoding.UTF8.GetByteCount(password));
                foreach (byte theByte in crypto)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }

        private Dictionary<string, object> GetDict(string json)
        {
            dynamic obj = JsonConvert.DeserializeObject<IDictionary<string, object>>(
                json, new JsonConverter[] {new NestedJsonConverter()});
            return obj;
        }

        private Dictionary<string, string> ConrvToStr(dynamic obj, string field = null)
        {
            var result = new Dictionary<string, string>();
            if (field != null)
            {
                foreach (KeyValuePair<string, object> kvp in obj[field])
                    result.Add(kvp.Key, kvp.Value?.ToString() ?? "");
            }
            else
            {
                foreach (KeyValuePair<string, object> kvp in obj) result.Add(kvp.Key, kvp.Value?.ToString() ?? "");
            }

            return result;
        }

        /// <summary>
        ///To set access token
        /// </summary>
        /// <param name="accessToken">the string of access token</param>
        public void SetAccessToken(string accessToken)
        {
            //Set the `access_token` received after a successful authentication."""
            this._accessToken = accessToken;
        }

        /// <summary>
        /// Get login url
        /// </summary>
        /// <returns>returns login url</returns>
        public string LoginUrl()
        {
            //Get the remote login url to which a user should be redirected
            //to initiate the login flow.
            return Login + "?api_key=" + this._apiKey;
        }

        /// <summary>
        ///     curl https://api.kite.trade/session/token
        ///     -d "api_key=xxx"
        ///     -d "request_token=yyy"
        ///     -d "checksum=zzz"
        ///     Authentication involves redirecting a user
        ///     to the public Kite login endpoint
        ///     https://kite.trade/connect/login?api_key=xxx.
        ///     A successful login comes
        ///     back with a request_token
        ///     as a URL query parameter to
        ///     the registered redirect url.
        /// Gets the accesstoken
        /// response contains not just the `access_token`, but metadata for
        /// the user who has authenticated.
        /// </summary>
        /// <param name="requestToken">The request token you got after login</param>
        /// <param name="secret">your API secret</param>
        /// <returns>
        /// A dictionary containing  lists
        ///    {
        ///"access_token": "f67c2bcbfcfa30fccb36f72dca22a817",
        ///"public_token": "5dd3f8d7c8aa91cff362de9f73212e28",
        ///"user_id": "AB0012",
        ///"user_type": "investor",
        ///"email": "kite@connect",
        ///"user_name": "Kite Connect",
        ///"login_time": "2015-12-19 17:16:36",
        ///"broker": "ZERODHA",
        ///"exchange": [
        ///  "MCXSXFO",
        ///  "MCXSXCM",
        ///  "BSE",
        ///  "NSE",
        ///  "BFO",
        ///  "NFO",
        ///  "NCDEX",
        ///  "MCX",
        ///  "CDS",
        ///  "MCXSX"
        ///],
        ///"product": [
        ///  "BO",
        ///  "CO",
        ///  "CNC",
        ///  "MIS",
        ///  "NRML"
        ///],
        ///"order_type": [
        ///  "LIMIT",
        ///  "MARKET",
        ///  "SL",
        ///  "SL-M"
        ///]
        ///}
        /// </returns>
        public dynamic RequestAccessToken(string requestToken, string secret)
        {
            //Do the token exchange with the `request_token` obtained after the login flow,
            //and retrieve the `access_token` required for all subsequent requests.The
            //response contains not just the `access_token`, but metadata for
            //the user who has authenticated.
            //- `request_token` is the token obtained from the GET paramers after a successful login redirect.
            //- `secret` is the API secret issued with the API key.
            // h = hashlib.sha256(self.api_key.encode("utf-8") + request_token.encode("utf-8") + secret.encode("utf-8"))
            string checksum = Sha256(this._apiKey + requestToken + secret);
            var values = new Dictionary<string, string>
            {
                {"request_token", requestToken},
                {"checksum", checksum}
            };
            dynamic resp = Post("api.validate", values);
            if ((resp != null) && resp.ContainsKey("access_token"))
            {
                SetAccessToken(resp["access_token"]);
            }
            return resp;
        }

        /// <summary>
        ///     curl --request DELETE \
        ///     "https://api.kite.trade/session/token?api_key=xxx&access_token=yyy"
        ///     This call invalidates the access_token
        ///     and destroys the API session. After this,
        ///     the user should be sent through a new login
        ///     flow before further interactions.
        /// </summary>
        /// <param name="accessToken">The access token receieved during login</param>
        public void InvalidateToken(string accessToken = null)
        {
            var values = new Dictionary<string, string>();

            if (accessToken != null)
            {
                values.Add("access_token", accessToken);
            }
            Delete("api.invalidate", values);
        }

        /// <summary>
        /// To get margin status of current segment 
        /// </summary>
        /// <param name="segment"> can be "equity" or "commodity" </param>
        /// <returns>
        /// A nested Dictionary
        /// {
        ///  "enabled": true,
        ///  "net": 0.0,
        ///  "available": {
        ///    "cash": 0.0,
        ///    "intraday_payin": 0.0,
        ///    "adhoc_margin": 0.0,
        ///    "collateral": 0.0
        ///  },
        ///  "utilised": {
        ///    "m2m_unrealised": 0.0,
        ///    "m2m_realised": 0.0,
        ///    "debits": 0.0,
        ///    "span": 0.0,
        ///    "option_premium": 0.0,
        ///    "holding_sales": 0.0,
        ///    "exposure": 23285.85,
        ///    "turnover": 0.0
        ///  }
        ///}
        /// </returns>
        public dynamic Margins(string segment)
        {
            //Get account balance and cash margin details for a particular segment.
            //- `segment` is the trading segment(eg: equity or commodity)
            var values = new Dictionary<string, string> {{"segment", segment}};
            return Get("user.margins", values);
        }
    }
}