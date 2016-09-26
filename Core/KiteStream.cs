using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using WebSocketSharp;
using KiteCore.Helpers;

namespace KiteCore.Core
{
    public class KiteStream
    {
        private readonly BlockingCollection<byte[]> _blockingCollection = new BlockingCollection<byte[]>();
        private readonly List<string> _subscription = new List<string>();
        private readonly Watchdog _watchDog;
        private readonly WebSocket _ws;
        private int _bytecounter;
        private int _retry;

        /// <summary>
        /// Initializes a new instance of the <see cref="KiteStream"/> class.
        /// This function sets up the websocket,watchdog timer
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="publicToken">The public token.</param>
        public KiteStream(string apiKey, string userId, string publicToken)
        {
            this._watchDog = new Watchdog(5);
            string urlstr = "wss://websocket.kite.trade/?api_key=" + apiKey + "&user_id=" + userId + "&public_token=" +
                            publicToken;
            this._ws = new WebSocket(urlstr) {Log = {Level = LogLevel.Error}};
            this._ws.OnOpen += (sender, e) =>
            {
                this._retry = 0;
                foreach (string x in this._subscription)
                {
                    this._ws.Send(x);
                }
            };
            this._ws.OnMessage += (sender, e) =>
            {
                this._watchDog.Reset();
                if (e.RawData.Length > 1)
                {
                    this._blockingCollection.Add(e.RawData);
                }
            };
            this._ws.OnClose += (sender, e) =>
            {
                this._watchDog.Reset(); //redundant
                if (e.Code == (ushort) CloseStatusCode.Away) // You should have an escape from the reconnecting loop.
                    return;

                if (this._retry < 10)
                {
                    this._retry++;
                    Thread.Sleep(1000);
                    this._ws.Connect();
                }
                else
                {
                    this._ws.Log.Error("The reconnecting has failed.");
                }
            };
            this._watchDog.OnTimerExpired += (sender, e) =>
            {
                //Console.WriteLine("there was a timeout");
                this._watchDog.Reset();
                this._ws.Close();
            };
            StartSocket();
        }

        /// <summary>
        /// Enables the timeout for watch dog to handle disconnects from websocket
        /// </summary>
        public void EnableTimeout()
        {
            this._watchDog.Go();
        }

        /// <summary>
        /// Subcribes to the tickers individually or as string that srperates each instrument with a ,
        /// </summary>
        /// <param name="ticker">The ticker eg "4995329"</param>
        /// <param name="mode">The mode 
        /// "ltp" LTP. Packet contains only the last traded price (8 bytes).
        ///"quote" Quote.Packet contains several fields excluding market depth (44 bytes).
        ///"full" Full.Packet contains several fields including market depth (164 bytes).</param>
        public void SubcribeTo(string ticker, string mode)
        {
            string tickerstr = "{\"a\":\"subscribe\",\"v\":[" + ticker + "]}";
            string modestr = "{\"a\":\"mode\",\"v\":[\"" + mode + "\",[" + ticker + "]]}"; ////check this
            this._subscription.Add(tickerstr);
            this._subscription.Add(modestr);
            this._ws.Send(tickerstr);
            this._ws.Send(modestr);
        }

        /// <summary>
        /// Starts the socket.
        /// </summary>
        private void StartSocket()
        {
            this._ws.Connect();
        }

        /// <summary>
        /// Gets the MSG from the collection.packet is added to que on websocket.onmessage in the constructor
        /// </summary>
        /// <returns>returns a byte array contining raw websocket data</returns>
        private byte[] GetMsg()
        {
            return this._blockingCollection.Take();
        }


        /// <summary>
        /// Copies the memory (c++ memcpy) 
        /// </summary>
        /// <param name="dest">The dest ptr</param>
        /// <param name="src">The source ptr</param>
        /// <param name="count">number of elemrnts to copy</param>
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        private static extern unsafe void CopyMemory(void* dest, void* src, int count);

        /// <summary>
        /// Serializes the specified struct to bin array
        /// </summary>
        /// <param name="xstruct"> the struct containing the data 
        /// </param>
        /// <returns></returns>
        public static unsafe byte[] Serialize(FullMode xstruct)
        {
            var buffer = new byte[164];
            fixed (void* d = &buffer[0])
            {
                void* s = &xstruct;
                CopyMemory(d, s, buffer.Length);
            }

            return buffer;
        }

        /// <summary>
        /// Serializes the specified struct to bin array
        /// </summary>
        /// <param name="xstruct"> the struct containing the data 
        /// </param>
        /// <returns></returns>
        public static unsafe byte[] Serialize(QuoteMode xstruct)
        {
            var buffer = new byte[164];
            fixed (void* d = &buffer[0])
            {
                void* s = &xstruct;
                CopyMemory(d, s, buffer.Length);
            }

            return buffer;
        }

        /// <summary>
        /// Serializes the specified struct to bin array
        /// </summary>
        /// <param name="xstruct"> the struct containing the data 
        /// </param>
        /// <returns></returns>
        public static unsafe byte[] Serialize(LtpMode xstruct)
        {
            var buffer = new byte[164];
            fixed (void* d = &buffer[0])
            {
                void* s = &xstruct;
                CopyMemory(d, s, buffer.Length);
            }

            return buffer;
        }

        /// <summary>
        /// De Serializes the specified array to struct
        /// </summary>
        /// <param name="bin">The byte array containing the data</param>
        /// <returns></returns>
        public static unsafe FullMode DeSerializeFull(byte[] bin)
        {
            FullMode yFullMode;

            fixed (byte* ptr = &bin[0])
            {
                yFullMode = *(FullMode*) ptr;
            }

            return yFullMode;
        }

        /// <summary>
        /// De Serializes the specified array to struct
        /// </summary>
        /// <param name="bin">The byte array containing the data</param>
        /// <returns></returns>
        public static unsafe QuoteMode DeSerializeQuote(byte[] bin)
        {
            QuoteMode yQuoteMode;

            fixed (byte* ptr = &bin[0])
            {
                yQuoteMode = *(QuoteMode*) ptr;
            }

            return yQuoteMode;
        }

        /// <summary>
        /// De Serializes the specified array to struct
        /// </summary>
        /// <param name="bin">The byte array containing the data</param>
        /// <returns></returns>
        public static unsafe LtpMode DeSerializeLtp(byte[] bin)
        {
            LtpMode yLtpMode;

            fixed (byte* ptr = &bin[0])
            {
                yLtpMode = *(LtpMode*) ptr;
            }

            return yLtpMode;
        }

        /// <summary>
        /// Stops the socket.
        /// </summary>
        public void StopSocket()
        {
            this._ws.Close();
        }

        /// <summary>
        /// Gets the int16 from byte array.
        /// </summary>
        /// <param name="arr">The byte array from which a ushort is to be extracted</param>
        /// <param name="index">The index in byte array from whare the ushort is to be extracted </param>
        /// <returns></returns>
        public ushort GetInt16(ref byte[] arr, int index)
        {
            unsafe
            {
                fixed (byte* a2Rr = &arr[index])
                {
                    var uint16Ptr = (ushort*) a2Rr;
                    ushort v = *uint16Ptr;
                    return BitConverter.IsLittleEndian ? ReverseBytes(v) : v;

                    //uint32Ptr = (UInt32*) ((byte*) uint32Ptr+6);
                }
            }
        }

        /// <summary>
        /// Gets the int32 from byte array.
        /// </summary>
        /// <param name="arr">The byte array from which a uint is to be extracted</param>
        /// <param name="index">The index in byte array from whare the uint is to be extracted </param>
        /// <returns></returns>
        public uint GetInt32(ref byte[] arr, int index)
        {
            unsafe
            {
                fixed (byte* a2Rr = &arr[index])
                {
                    var uint32Ptr = (uint*) a2Rr;
                    uint v = *uint32Ptr;
                    return BitConverter.IsLittleEndian ? ReverseBytes(v) : v;
                    //uint32Ptr = (UInt32*) ((byte*) uint32Ptr+6);
                }
            }
        }

        /// <summary>
        /// Reverses the bytes to adjust for endianness
        /// </summary>
        /// <param name="value">The int that is to be adjusted for endianness</param>
        /// <returns> The adjusted int
        /// </returns>
        public uint ReverseBytes(uint value)
        {
            return ((value & 0x000000FFU) << 24) | ((value & 0x0000FF00U) << 8) |
                   ((value & 0x00FF0000U) >> 8) | ((value & 0xFF000000U) >> 24);
        }

        /// <summary>
        /// Reverses the bytes for endianness
        /// </summary>
        /// <param name="value">The ushort that is to be adjusted for endianness</param>
        /// <returns> The adjusted ushort
        /// </returns>
        public ushort ReverseBytes(ushort value)
        {
            return (ushort) (((value & 0xFFU) << 8) | ((value & 0xFF00U) >> 8));
        }

        /// <summary>
        /// Reverses the bytes for endianness
        /// </summary>
        /// <param name="value">The ulong that is to be adjusted for endianness</param>
        /// <returns> The adjusted ulong
        /// </returns>
        public ulong ReverseBytes(ulong value)
        {
            return ((value & 0x00000000000000FFUL) << 56) | ((value & 0x000000000000FF00UL) << 40) |
                   ((value & 0x0000000000FF0000UL) << 24) | ((value & 0x00000000FF000000UL) << 8) |
                   ((value & 0x000000FF00000000UL) >> 8) | ((value & 0x0000FF0000000000UL) >> 24) |
                   ((value & 0x00FF000000000000UL) >> 40) | ((value & 0xFF00000000000000UL) >> 56);
        }

        /// <summary>
        /// decodes the byte array from websocket into strucrures of the different modes(ltp,quote,full) using pointer magic**
        /// This does all the work
        /// </summary>
        /// <returns></returns>
        public List<dynamic> Getpackets()
        {
            unsafe
            {
                var lst = new List<dynamic>();
                this._bytecounter = 0;
                byte[] binary = GetMsg();

                if (binary.Length == 15) //some errors in websocket data bypass corrupt data
                {
                    lst.Clear();
                    lst.Add("error");
                    return lst;
                }

                ushort noofpakets = GetInt16(ref binary, 0);
                this._bytecounter += 2;
                for (var i = 0; i < noofpakets; i++)
                {
                    if (binary.Length < this._bytecounter)
                    {
                        lst.Clear();
                        lst.Add("error");
                        return lst;
                    }
                    ushort noofbytesinpkt = GetInt16(ref binary, this._bytecounter);
                    this._bytecounter += 2;
                    switch (noofbytesinpkt)
                    {
                        case 8:
                            LtpMode ltp;
                            fixed (byte* ptr = &binary[this._bytecounter])
                            {
                                ltp = *(LtpMode*) ptr;
                            }
                            if (BitConverter.IsLittleEndian)
                            {
                                ltp.Instrumenttocken = ReverseBytes(ltp.Instrumenttocken);
                                ltp.Ltp = ReverseBytes(ltp.Ltp);
                            }
                            lst.Add(ltp);
                            this._bytecounter += 8;
                            break;

                        case 44:
                            QuoteMode quote;
                            fixed (byte* ptr = &binary[this._bytecounter])
                            {
                                quote = *(QuoteMode*) ptr;
                            }
                            if (BitConverter.IsLittleEndian)
                            {
                                quote.Instrumenttocken = ReverseBytes(quote.Instrumenttocken);
                                quote.Ltp = ReverseBytes(quote.Ltp);
                                quote.Lasttradedquantity = ReverseBytes(quote.Lasttradedquantity);
                                quote.Averagetradedprice = ReverseBytes(quote.Averagetradedprice);
                                quote.Volumetradedfortheday = ReverseBytes(quote.Volumetradedfortheday);
                                quote.Totalbuyquantity = ReverseBytes(quote.Totalbuyquantity);
                                quote.Totalsellquantity = ReverseBytes(quote.Totalsellquantity);
                                quote.Openprice = ReverseBytes(quote.Openprice);
                                quote.Highprice = ReverseBytes(quote.Highprice);
                                quote.Lowprice = ReverseBytes(quote.Lowprice);
                                quote.Closeprice = ReverseBytes(quote.Closeprice);
                            }
                            lst.Add(quote);
                            this._bytecounter += 44;
                            break;

                        case 164:
                            FullMode full;
                            fixed (byte* ptr = &binary[this._bytecounter])
                            {
                                full = *(FullMode*) ptr;
                            }
                            if (BitConverter.IsLittleEndian)
                            {
                                full.Instrumenttocken = ReverseBytes(full.Instrumenttocken); //4 bytes
                                full.Ltp = ReverseBytes(full.Ltp); //4 byte
                                full.Lasttradedquantity = ReverseBytes(full.Lasttradedquantity);
                                full.Averagetradedprice = ReverseBytes(full.Averagetradedprice);
                                full.Volumetradedfortheday = ReverseBytes(full.Volumetradedfortheday);
                                full.Totalbuyquantity = ReverseBytes(full.Totalbuyquantity);
                                full.Totalsellquantity = ReverseBytes(full.Totalsellquantity);
                                full.Openprice = ReverseBytes(full.Openprice);
                                full.Highprice = ReverseBytes(full.Highprice);
                                full.Lowprice = ReverseBytes(full.Lowprice);
                                full.Closeprice = ReverseBytes(full.Closeprice);

                                //bid
                                full.Quantitybid1 = ReverseBytes(full.Quantitybid1);
                                full.Pricebid1 = ReverseBytes(full.Pricebid1);
                                full.Ordersbid1 = ReverseBytes(full.Ordersbid1);

                                full.Quantitybid2 = ReverseBytes(full.Quantitybid2);
                                full.Pricebid2 = ReverseBytes(full.Pricebid2);
                                full.Ordersbid2 = ReverseBytes(full.Ordersbid2);

                                full.Quantitybid3 = ReverseBytes(full.Quantitybid3);
                                full.Pricebid3 = ReverseBytes(full.Pricebid3);
                                full.Ordersbid3 = ReverseBytes(full.Ordersbid3);

                                full.Quantitybid4 = ReverseBytes(full.Quantitybid4);
                                full.Pricebid4 = ReverseBytes(full.Pricebid4);
                                full.Ordersbid4 = ReverseBytes(full.Ordersbid4);

                                full.Quantitybid5 = ReverseBytes(full.Quantitybid5);
                                full.Pricebid5 = ReverseBytes(full.Pricebid5);
                                full.Ordersbid5 = ReverseBytes(full.Ordersbid5);

                                //ask
                                full.Quantityask1 = ReverseBytes(full.Quantityask1);
                                full.Priceask1 = ReverseBytes(full.Priceask1);
                                full.Ordersask1 = ReverseBytes(full.Ordersask1);

                                full.Quantityask2 = ReverseBytes(full.Quantityask2);
                                full.Priceask2 = ReverseBytes(full.Priceask2);
                                full.Ordersask2 = ReverseBytes(full.Ordersask2);

                                full.Quantityask3 = ReverseBytes(full.Quantityask3);
                                full.Priceask3 = ReverseBytes(full.Priceask3);
                                full.Ordersask3 = ReverseBytes(full.Ordersask3);

                                full.Quantityask4 = ReverseBytes(full.Quantityask4);
                                full.Priceask4 = ReverseBytes(full.Priceask4);
                                full.Ordersask4 = ReverseBytes(full.Ordersask4);

                                full.Quantityask5 = ReverseBytes(full.Quantityask5);
                                full.Priceask5 = ReverseBytes(full.Priceask5);
                                full.Ordersask5 = ReverseBytes(full.Ordersask5);
                            }
                            lst.Add(full);
                            this._bytecounter += 164;
                            break;
                    }
                }
                return lst;
            }
        }

        /// <summary>
        /// ltp structure in sequential format.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct LtpMode
        {
            public uint Instrumenttocken; //4 bytes
            public uint Ltp; //4 byte
        }

        /// <summary>
        /// quote structure in sequential format.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct QuoteMode
        {
            public uint Instrumenttocken; //4 bytes
            public uint Ltp; //4 byte
            public uint Lasttradedquantity;
            public uint Averagetradedprice;
            public uint Volumetradedfortheday;
            public uint Totalbuyquantity;
            public uint Totalsellquantity;
            public uint Openprice;
            public uint Highprice;
            public uint Lowprice;
            public uint Closeprice;
        }

        /// <summary>
        /// full structure in sequential format.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct FullMode
        {
            public uint Instrumenttocken; //4 bytes
            public uint Ltp; //4 byte
            public uint Lasttradedquantity;
            public uint Averagetradedprice;
            public uint Volumetradedfortheday;
            public uint Totalbuyquantity;
            public uint Totalsellquantity;
            public uint Openprice;
            public uint Highprice;
            public uint Lowprice;
            public uint Closeprice;
            public uint Quantitybid1;
            public uint Pricebid1;
            public uint Ordersbid1;
            public uint Quantitybid2;
            public uint Pricebid2;
            public uint Ordersbid2;
            public uint Quantitybid3;
            public uint Pricebid3;
            public uint Ordersbid3;
            public uint Quantitybid4;
            public uint Pricebid4;
            public uint Ordersbid4;
            public uint Quantitybid5;
            public uint Pricebid5;
            public uint Ordersbid5;
            public uint Quantityask1;
            public uint Priceask1;
            public uint Ordersask1;
            public uint Quantityask2;
            public uint Priceask2;
            public uint Ordersask2;
            public uint Quantityask3;
            public uint Priceask3;
            public uint Ordersask3;
            public uint Quantityask4;
            public uint Priceask4;
            public uint Ordersask4;
            public uint Quantityask5;
            public uint Priceask5;
            public uint Ordersask5;
        }
    }
}