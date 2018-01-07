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
}
