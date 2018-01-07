using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCOM4Lib;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json.Linq;


namespace SmartcomConnector.ATrade
{
    class TickerInfoServer
    {
        public TickerInfoServer(StServer smartCom, string endpoint)
        {
            symbols = new SortedDictionary<string, TickerInfo>();
            server = smartCom;
            this.endpoint = endpoint;
            poller = new NetMQPoller();
            socket = new ResponseSocket();
            socket.Bind(endpoint);

            socket.ReceiveReady += (s, a) =>
            {
                List<string> parts = a.Socket.ReceiveMultipartStrings(1);
                string msg = parts[0];
                handleMessage(msg, a.Socket);
            };

            server.AddSymbol += Server_AddSymbol;
        }

        public void Start()
        {
            poller.RunAsync();
        }

        public void Stop()
        {
            poller.Stop();
        }

        private void Server_AddSymbol(int row, int nrows, string symbol, string short_name, string long_name, string type, int decimals, int lot_size, double punkt, double step, string sec_ext_id, string sec_exch_name, DateTime expiry_date, double days_before_expiry, double strike)
        {
            lock(symbols)
            {
                symbols.Add(symbol, new TickerInfo(symbol, step, punkt, lot_size));
            }
        }

        private void handleMessage(string msg, NetMQSocket socket)
        {
            JObject req = JObject.Parse(msg);
            string tickerId = (string)req["ticker"];
            TickerInfo info;
            bool found = false;
            lock(symbols)
            {
                found = symbols.TryGetValue(tickerId, out info);
            }
            if(found)
            {
                JObject resp = new JObject();
                resp["ticker"] = info.TickerId;
                resp["lot_size"] = info.LotSize;
                resp["tick_size"] = info.TickSize;
                resp["tick_price"] = info.TickPrice;
                socket.SendMoreFrame("OK");
                socket.SendFrame(resp.ToString());
            }
            else
            {
                socket.SendMoreFrame("ERROR");
                socket.SendFrame("Unknown ticker");
            }
        }

        private StServer server;
        string endpoint;
        NetMQSocket socket;
        NetMQPoller poller;
        SortedDictionary<string, TickerInfo> symbols;
    }
}
