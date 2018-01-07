using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartcomConnector.ATrade
{
    [Serializable]
    public class InvalidStateException : Exception
    {
        public InvalidStateException() { }
        public InvalidStateException(string message) : base(message) { }
        public InvalidStateException(string message, Exception inner) : base(message, inner) { }
        protected InvalidStateException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public struct Tick
    {
        public Tick(string ticker, Datatype datatype, DateTime timestamp, double value, double volume)
        {
            Ticker = ticker;
            Type = datatype;
            Timestamp = timestamp;
            Value = value;
            Volume = volume;
        }

        public string Ticker;
        public Datatype Type;
        public DateTime Timestamp;
        public double Value;
        public double Volume;
    }

    public static class TickSerializer
    {
        private static DateTime epoch = new DateTime(1970, 1, 1);

        public static void WriteToStream(Stream stream, Tick tick)
        {
            var delta = tick.Timestamp.Subtract(epoch);
            long ts = (long)delta.TotalSeconds;
            int fracTs = 1000 * delta.Milliseconds;

            WriteBytes(stream, BitConverter.GetBytes(1));
            WriteBytes(stream, BitConverter.GetBytes(ts));
            WriteBytes(stream, BitConverter.GetBytes(fracTs));

            WriteBytes(stream, BitConverter.GetBytes((long)Math.Floor(tick.Value)));
            WriteBytes(stream, BitConverter.GetBytes((int)(tick.Value - Math.Floor(tick.Value))));
            WriteBytes(stream, BitConverter.GetBytes((int)tick.Volume));
        }

        public static void WriteBytes(Stream stream, byte[] b)
        {
            stream.Write(b, 0, b.Length);
        }
    }

    public struct Bar
    {
        public Bar(DateTime ts, double open, double high, double low, double close, long volume)
        {
            Timestamp = ts;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }
        public DateTime Timestamp;
        public double Open;
        public double High;
        public double Low;
        public double Close;
        public long Volume;
    }

    public static class BarSerializer
    {
        private static DateTime epoch = new DateTime(1970, 1, 1);

        public static void WriteToStream(Stream stream, Bar b)
        {
            long ts = (long)b.Timestamp.Subtract(epoch).TotalSeconds;

            WriteBytes(stream, BitConverter.GetBytes(ts));
            WriteBytes(stream, BitConverter.GetBytes(b.Open));
            WriteBytes(stream, BitConverter.GetBytes(b.High));
            WriteBytes(stream, BitConverter.GetBytes(b.Low));
            WriteBytes(stream, BitConverter.GetBytes(b.Close));
            WriteBytes(stream, BitConverter.GetBytes(b.Volume));
        }

        public static void WriteBytes(Stream stream, byte[] b)
        {
            stream.Write(b, 0, b.Length);
        }
    }

    public struct TickerInfo
    {
        public TickerInfo(string tickerId, double tickSize, double tickPrice, int lotSize)
        {
            TickerId = tickerId;
            TickSize = tickSize;
            TickPrice = tickPrice;
            LotSize = lotSize;
        }
        public string TickerId;
        public double TickSize;
        public double TickPrice;
        public int LotSize;
    }

    public enum Datatype
    {
        LastTradePrice = 1,
        OpenInterest = 3,
        BestBid = 4,
        BestOffer = 5,
        Depth = 6,
        TheoryPrice = 7,
        Volatility = 8,
        TotalSupply = 9,
        TotalDemand = 10
    }
}
