###KiteCore
kite connect and websocket library in C#

Follow this demo you will get it.

P.S-->
please un comment kitecon.OrderPlace in code below when you want to test the order placement.To dummy test it do it in off market hours
so it will get rejected automatically

``` C#
//This demo shows basic api usage
//The references you need to add to use lib
//Newtonsoft.Json 8.0.0.0
//RestSharp  105.2.3.0
//websocket-sharp 1.0.2.22894
namespace KiteCore
{
    public class Demo
    {
        private static void Main(string[] args)
        {
            //Create the kite connect variable
            KiteConnect kitecon;

            //kindly enter "API key", "API secret"(replace them with the strings from your kite api
            //when you start the program you will get a webbrowser dialogue(Thanks to ishwarm) 
            //there you login and the program saves the access token and you are good to go.
            //This also initializes the kite connect variable
            //and saves user ID and public token to PrgConstants class for use later
            new Login().Initiate(out kitecon, "API key", "API secret");

            //Download market instruments to desktop
            Console.WriteLine();
            Console.WriteLine("saving instruments available to trade");
            DataHelpers.Storeinstruments(ref kitecon);

            //Give message box of the equity margins
            MessageBox.Show(kitecon.Margins("equity")["net"].ToString());

            //Retrive a market quote and displays last price
            Console.WriteLine();
            Console.WriteLine("A market quote" + kitecon.Quote("NSE", "BHARATFIN")["last_price"]);

            //Retrive series of historical quotes on 15 min interval and displays time and last price
            Console.WriteLine();
            Console.WriteLine("retrieving historical quotes");
            JArray temp = kitecon.Historical("4995329", "15minute", 1);
            foreach (JToken x in temp)
            {
                Console.WriteLine();
                Console.WriteLine("time");
                Console.WriteLine(x[(int) DataHelpers.Ohlc.Time]);
                Console.WriteLine("close");
                Console.WriteLine(x[(int) DataHelpers.Ohlc.Close]);
            }

            //Place a buy order and loop till complete
            Console.WriteLine();
            Console.WriteLine("Placing buy order");
            //var buyOdrId = kitecon.OrderPlace("NSE", "BHARATFIN", "BUY", "1", "MARKET", "MIS", "DAY");
            //Console.WriteLine("buyOdrId"+ buyOdrId);
            //while (kitecon.Orders(buyOdrId)[0]["status"] != "COMPLETE")
            //{
            //}


            //Place a sell order and loop till complete
            Console.WriteLine();
            Console.WriteLine("Placing sell order");
            //var sellOdrId = kitecon.OrderPlace("NSE", "BHARATFIN", "SELL", "1", "MARKET", "MIS", "DAY");
            //Console.WriteLine("sellOdrId"+ sellOdrId);
            //while (kitecon.Orders(sellOdrId)[0]["status"] != "COMPLETE")
            //{
            //}

            //Initiate the kite stream
            //The parameteres saved during login are used here
            KiteStream stream = new KiteStream(PrgConstants.ApiKey, PrgConstants.UserId, PrgConstants.PublicToken);

            //Enable Timeout to handle interruption in websocket connection
            stream.EnableTimeout();

            //Subcribe to a instrument using the instrument token in the file stored using DataHelpers.Storeinstruments
            stream.SubcribeTo("4995329", "ltp");

            //Display the Quotes receieved on websocket
            Console.WriteLine();
            Console.WriteLine("displaying Ltp Quotes");
            for (int i = 0; i < 50; i++)
            {
                Thread.Sleep(1000);

                //Get the decoded packets from stream these decoded packets are in form of structs
                List<dynamic> lst = stream.Getpackets();
                foreach (dynamic xLtpMode in lst)
                {
                    if (!(xLtpMode is KiteStream.LtpMode)) continue;
                    Console.WriteLine();
                    Console.WriteLine("Instrumenttocken");
                    Console.WriteLine(xLtpMode.Instrumenttocken); //4 bytes
                    Console.WriteLine("Ltp");
                    Console.WriteLine(Convert.ToDouble(xLtpMode.Ltp)/100); //4 byte
                }
            }
            Console.WriteLine();
            Console.WriteLine("end of demo at" + DateTime.Now.TimeOfDay + " press enter");
            Console.ReadLine();
        }
    }
}
```
