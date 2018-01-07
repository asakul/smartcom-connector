using NetMQ;
using NetMQ.Sockets;
using SmartCOM4Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartcomConnector.ATrade
{
    class QuoteSourceServer
    {
        public QuoteSourceServer(List<string> symbols, StServer smartCom, string endpoint)
        {
            server = smartCom;
            socket = new PublisherSocket();

            server.UpdateQuote += OnUpdateQuote;
            server.AddTick += OnAddTick;            
        }

        private void OnAddTick(string symbol, DateTime datetime, double price, double volume, string tradeno, StOrder_Action action)
        {
            lock (tickQueue)
            {
                tickQueue.Enqueue(new Tick(symbol, Datatype.LastTradePrice, datetime, price, volume));
            }
        }

        private void OnUpdateQuote(string symbol, DateTime datetime, double open, double high, double low, double close, double last, double volume, double size, double bid, double ask, double bidsize, double asksize, double open_int, double go_buy, double go_sell, double go_base, double go_base_backed, double high_limit, double low_limit, int trading_status, double volat, double theor_price, double step_price)
        {
            double lastBid = bids[symbol];
            double lastAsk = asks[symbol];
            double lastOpenInt = openInts[symbol];

            lock (tickQueue)
            {
                if (lastBid != bid)
                {
                    tickQueue.Enqueue(new Tick(symbol, Datatype.BestBid, datetime, bid, 0));
                    bids[symbol] = bid;
                }
                if (lastAsk != ask)
                {
                    tickQueue.Enqueue(new Tick(symbol, Datatype.BestOffer, datetime, ask, 0));
                    asks[symbol] = ask;
                }
                if (lastOpenInt != open_int)
                {
                    tickQueue.Enqueue(new Tick(symbol, Datatype.OpenInterest, datetime, open_int, 0));
                    openInts[symbol] = open_int;
                }
            }
        }

        public void Start()
        {
            foreach (var symbol in symbols)
            {
                server.ListenQuotes(symbol);
            }
            running = true;
            Task.Factory.StartNew(SendingTask);
        }

        public void Stop()
        {
            foreach (var symbol in symbols)
            {
                server.CancelQuotes(symbol);
            }
            running = false;
        }

        private void SendingTask()
        {
            while (running)
            {
                if (tickQueue.Count == 0)
                {
                    Thread.Sleep(10);
                }
                else
                {
                    List<Tick> ticks = GetTicks(16);
                    while (ticks.Count > 0)
                    {
                        string currentSymbol = ticks[0].Ticker;
                        var currentTicks = ticks.FindAll((t) => { return t.Ticker == currentSymbol; });
                        SendTicks(currentSymbol, currentTicks, socket);
                        ticks.RemoveAll((t) => { return t.Ticker == currentSymbol; });
                    }
                }
            }
        }

        private void SendTicks(string ticker, List<Tick> ticks, NetMQSocket sock)
        {
            var stream = new MemoryStream();
            foreach (var tick in ticks)
            {
                TickSerializer.WriteToStream(stream, tick);
            }

            sock.SendMoreFrame(ticker);
            sock.SendFrame(stream.ToArray());
        }

        private List<Tick> GetTicks(int maxTicks)
        {
            var list = new List<Tick>();

            lock (tickQueue)
            {
                int counter = 0;
                while (tickQueue.Count > 0)
                {
                    list.Add(tickQueue.Dequeue());
                    counter++;
                    if (counter >= maxTicks)
                        break;
                }
            }

            return list;
        }

        private StServer server;
        private NetMQSocket socket;
        private List<string> symbols;
        private Dictionary<string, double> bids;
        private Dictionary<string, double> asks;
        private Dictionary<string, double> openInts;

        private bool running;
        private Queue<Tick> tickQueue;
    }
}
