using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SmartCOM4Lib;
using SmartcomConnector.ATrade;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SmartComConnectorTests
{
    [TestClass]
    public class TestHistoryProviderServer
    {
        UTF8Encoding utf8 = new UTF8Encoding();
        HistoryProviderServer hsp;
        Mock<StServer> smartCom;

        [TestInitialize]
        public void SetUp()
        {
            smartCom = new Mock<StServer>();
            hsp = new HistoryProviderServer(smartCom.Object, "inproc://hsp");
            hsp.Start();
        }

        [TestCleanup]
        public void TearDown()
        {
            hsp.Stop();
        }

        [TestMethod]
        public void TestOneBarRequest()
        {
            smartCom.Setup(f => f.GetBars("FOO", StBarInterval.StBarInterval_1Min, new DateTime(2017, 12, 31), It.IsAny<int>())).Raises(f =>
                f.AddBar += null, 0, 1, "FOO", StBarInterval.StBarInterval_1Min, new DateTime(2017, 12, 31, 10, 00, 00), 10, 12, 9, 11, 1000, 200000);

            RequestSocket sock = new RequestSocket(">inproc://hsp");
            JObject req = new JObject();
            req["ticker"] = "FOO";
            req["from"] = "2017-01-01";
            req["to"] = "2017-12-31";
            req["timeframe"] = "M1";
            sock.SendFrame(req.ToString());
            List<byte[]> respParts = new List<byte[]>();
            bool rc = sock.TryReceiveMultipartBytes(TimeSpan.FromSeconds(5), ref respParts);
            Assert.IsTrue(rc);
            Assert.AreEqual(2, respParts.Count);
            Assert.AreEqual("OK", utf8.GetString(respParts[0]));

            MemoryStream stream = new MemoryStream();
            BarSerializer.WriteToStream(stream, new Bar(new DateTime(2017, 12, 31, 10, 00, 00), 10, 12, 9, 11, 1000));

            CollectionAssert.AreEqual(stream.ToArray(), respParts[1]);
        }
    }
}
