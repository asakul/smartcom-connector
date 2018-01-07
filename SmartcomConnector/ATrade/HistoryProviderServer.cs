using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SmartCOM4Lib;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json.Linq;
using System.IO;

namespace SmartcomConnector.ATrade
{
    public class HistoryProviderServer
    {
        public HistoryProviderServer(StServer smartCom, string endpoint)
        {
            data = new List<Bar>();
            dataSemaphore = new SemaphoreSlim(1, 1);
            server = smartCom;
            hpEndpoint = endpoint;
            poller = new NetMQPoller();
            socket = new ResponseSocket();
            socket.Bind(endpoint);

            socket.ReceiveReady += (s, a) =>
            {
                List<string> parts = a.Socket.ReceiveMultipartStrings(1);
                string msg = parts[0];
                handleMessage(msg, a.Socket);
            };

            server.AddBar += Server_AddBar;
        }

        private void Server_AddBar(int row, int nrows, string symbol, StBarInterval interval, DateTime datetime, double open, double high, double low, double close, double volume, double open_int)
        {
            data.Add(new Bar(datetime, open, high, low, close, (long)volume));
            if (row >= nrows - 1)
            {
                dataSemaphore.Release();
            }
        }

        public void Start()
        {
            poller.RunAsync();
        }

        public void Stop()
        {
            poller.Stop();
            socket.Close();
        }

        private void handleMessage(string msg, NetMQSocket sock)
        {
            JObject req = JObject.Parse(msg);
            string ticker = (string)req["ticker"];
            string fromRaw = (string)req["from"];
            string toRaw = (string)req["to"];
            string timeframeRaw = (string)req["timeframe"];
            DateTime firstTime = parseDate(fromRaw);
            DateTime lastTime = parseDate(toRaw);
            StBarInterval timeframe = parseSmartComTimeFrame(timeframeRaw);
            int countBars = calculateCountBars(firstTime, lastTime, timeframe);

            data.Clear();

            server.GetBars(ticker, timeframe, lastTime, countBars);

            dataSemaphore.Wait();

            sendResponse(data, sock);
        }

        private DateTime parseDate(string s)
        {
            string[] parts = s.Split('-');
            int year = Int32.Parse(parts[0]);
            int month = Int32.Parse(parts[1]);
            int day = Int32.Parse(parts[2]);

            return new DateTime(year, month, day);
        }

        private StBarInterval parseSmartComTimeFrame(string timeframeRaw)
        {
            switch(timeframeRaw)
            {
                case "M1":
                    return StBarInterval.StBarInterval_1Min;
                case "M5":
                    return StBarInterval.StBarInterval_5Min;
                case "M10":
                    return StBarInterval.StBarInterval_10Min;
                case "M15":
                    return StBarInterval.StBarInterval_15Min;
                case "M30":
                    return StBarInterval.StBarInterval_30Min;
                case "H1":
                    return StBarInterval.StBarInterval_60Min;
                case "H2":
                    return StBarInterval.StBarInterval_2Hour;
                case "H4":
                    return StBarInterval.StBarInterval_4Hour;
                case "D":
                    return StBarInterval.StBarInterval_Day;
                case "MN":
                    return StBarInterval.StBarInterval_Month;

            }
            throw new ArgumentException("Invalid timeframe format");
        }

        private int calculateCountBars(DateTime start, DateTime end, StBarInterval timeframe)
        {
            return (int)end.Subtract(start).TotalSeconds / secondsInInterval(timeframe);
        }

        private int secondsInInterval(StBarInterval interval)
        {
            switch (interval)
            {
                case StBarInterval.StBarInterval_1Min:
                    return 60;
                case StBarInterval.StBarInterval_5Min:
                    return 60 * 5;
                case StBarInterval.StBarInterval_10Min:
                    return 60 * 10;
                case StBarInterval.StBarInterval_15Min:
                    return 60 * 15;
                case StBarInterval.StBarInterval_30Min:
                    return 60 * 30;
                case StBarInterval.StBarInterval_60Min:
                    return 60 * 60;
                case StBarInterval.StBarInterval_2Hour:
                    return 60 * 60 * 2;
                case StBarInterval.StBarInterval_4Hour:
                    return 60 * 60 * 4;
                case StBarInterval.StBarInterval_Day:
                    return 60 * 60 * 24;
                case StBarInterval.StBarInterval_Month:
                    return 60 * 60 * 24 * 30;
                default:
                    throw new ArgumentException("Not supported: " + interval.ToString());
            }
        }

        private void sendResponse(List<Bar> data, NetMQSocket sock)
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                foreach (Bar b in data)
                {
                    BarSerializer.WriteToStream(stream, b);
                }

                sock.SendMoreFrame("OK");
                sock.SendFrame(stream.ToArray());
            }
            catch(Exception e)
            {
                sock.SendMoreFrame("ERROR");
                sock.SendFrame(e.ToString());
            }
        }

        private StServer server;
        private string hpEndpoint;
        private NetMQPoller poller;
        private ResponseSocket socket;
        private SemaphoreSlim dataSemaphore;
        private List<Bar> data;
    }
}
