using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SmartCOM4Lib;
using SmartcomConnector.ATrade;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json.Linq;

namespace SmartComConnectorTests
{
    [TestClass]
    public class TestHistoryProviderServer
    {
        [TestMethod]
        public void TestConnection()
        {
            var smartCom = new Mock<StServer>();
            var hsp = new HistoryProviderServer((StServer)smartCom, "inproc://hsp");

            smartCom.Setup(f => f.GetBars("FOO", StBarInterval.StBarInterval_1Min, new DateTime(2017, 12, 31), 100)).Raises(f =>
                f.AddBar += null, 0, 1, "FOO", StBarInterval.StBarInterval_1Min, new DateTime(2017, 12, 31, 10, 00, 00), 10, 12, 9, 11, 1000, 200000);

            hsp.Start();

            RequestSocket sock = new RequestSocket(">inproc://hsp");
            JObject req = new JObject();
            req["ticker"] = "FOO";
            req["from"] = "2017-01-01";
            req["to"] = "2017-12-31";
            req["timeframe"] = "M1";
            sock.SendFrame(req.ToString());
            var respParts = sock.ReceiveMultipartBytes();
            Assert.AreEqual(respParts.Count, 2);
            Assert.AreEqual(respParts[0], "OK");
        }
    }
}
