using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using KiteCore.Core;
using Newtonsoft.Json;

namespace KiteCore.Helpers
{
    public static class DataHelpers
    {
        /// <summary>
        /// enum which is used to access results from kitecon.Historical
        /// </summary>
        public enum Ohlc
        {
            Time = 0,
            Open = 1,
            High,
            Low,
            Close
        }

        /// <summary>
        /// Saves access token and other meta data to a file on desktop
        /// </summary>
        /// <param name="kitecon">The initialized instance of kite connect</param>
        /// <param name="reqtoken">The request token receieved after login</param>
        /// <returns></returns>
        public static dynamic Saveaccesstoken(ref KiteConnect kitecon, string reqtoken)
        {
            dynamic data = kitecon.RequestAccessToken(reqtoken,
                PrgConstants.Apisecret);
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "data.txt");
            using (StreamWriter file = File.CreateText(path))
            {
                foreach (dynamic item in data)
                    file.WriteLine("{0}={1}", item.Key, item.Value);
            }
            return data;
        }

        /// <summary>
        /// stores the market instruments to desktop
        /// </summary>
        /// <param name="kitecon">The initialized instance of kite connect</param>
        public static void Storeinstruments(ref KiteConnect kitecon)
        {
            kitecon.StoreInstruments(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "InstrumentList.csv"));
        }
    }

    public class Login
    {
        /// <summary>
        /// Initiates the login , the kite connect variable and stores the meta data to PrgConstants class for later use
        /// </summary>
        /// <param name="kitecon">The uninitialized instance of kite connect</param>
        /// <param name="apiKey">your API key</param>
        /// <param name="apisecret">your API secret</param>
        public void Initiate(out KiteConnect kitecon, string apiKey, string apisecret)
        {
            PrgConstants.ApiKey = apiKey;
            PrgConstants.Apisecret = apisecret;
            kitecon = new KiteConnect(apiKey);
            Process.Start("https://kite.trade/connect/login?api_key=" + apiKey);
            Console.WriteLine("kindly enter req token");
            string token = Console.ReadLine();
            dynamic data = DataHelpers.Saveaccesstoken(ref kitecon, token);
            PrgConstants.AccessToken = data["access_token"];
            PrgConstants.PublicToken = data["public_token"];
            PrgConstants.UserId = data["user_id"];
            kitecon.SetAccessToken(PrgConstants.AccessToken);
        }
    }
}