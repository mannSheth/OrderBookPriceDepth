using System.Runtime.InteropServices;
using System.Text;

namespace cs
{
    //Parser that turns the binary into a human readable format

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Header
    {
        public int SeqNum;

        public int MsgSize;

        public char MsgType;

        public override string ToString()
        {
            return $"{SeqNum}/{MsgType}/";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 31)]
    public class OrderAdd
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] SymbolBuf;

        public string Symbol => Encoding.UTF8.GetString(SymbolBuf);

        public long OrderId;

        public char Side;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Reserved1;

        public long Volume;

        public int Price;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Reserved2;

        public override string ToString()
        {
            return $"{{'symbol': '{Symbol}', 'order_id': {OrderId}, 'side': '{Side}', 'volume': {Volume}, 'price': {Price}}}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 15)]
    public class OrderDelete
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] SymbolBuf;

        public string Symbol => Encoding.UTF8.GetString(SymbolBuf);

        public long OrderId;

        public char Side;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Reserved1;

        public override string ToString()
        {
            return $"{{'symbol': '{Symbol}', 'order_id': {OrderId}, 'side': '{Side}'}}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 23)]
    public class OrderTrade
    {
        //is this even needed, not too sure?
        public const int Size = 23;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] SymbolBuf;

        public string Symbol => Encoding.UTF8.GetString(SymbolBuf);

        public long OrderId;

        public char Side;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Reserved1;

        public long Volume;

        public override string ToString()
        {
            return $"{{'symbol': '{Symbol}', 'order_id': {OrderId}, 'side': '{Side}', 'volume': {Volume}}}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 31)]
    public class OrderUpdate
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] SymbolBuf;

        public string Symbol => Encoding.UTF8.GetString(SymbolBuf);

        public long OrderId;

        public char Side;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Reserved1;

        public long Volume;

        public int Price;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Reserved2;

        public override string ToString()
        {
            return $"{{'symbol': '{Symbol}', 'order_id': {OrderId}, 'side': '{Side}', 'volume': {Volume}, 'price': {Price}}}";
        }
    }

    public static class Message
    {
        public static (Header header, object Message) ReadNext(Stream s)
        {
            try
            {
                var header = ReadStruct<Header>(s);

                if (header == null)
                {
                    //EOF
                    return (null, null);
                }

                switch (header.MsgType)
                {
                    case 'A': return (header, ReadStruct<OrderAdd>(s));
                    case 'E': return (header, ReadStruct<OrderTrade>(s));
                    case 'D': return (header, ReadStruct<OrderDelete>(s));
                    case 'U': return (header, ReadStruct <OrderUpdate>(s));
                    default:
                        throw new ArgumentException($"unknown message type: {header.MsgType}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e}");
                throw;
            }
        }

        private static T? ReadStruct<T>(Stream s)
            where T : new()
        {
            var sz = Marshal.SizeOf<T>();
            var buf = new byte[sz];
            var read = s.Read(buf, 0, sz);
            if (read == sz)
            {
                var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
                var msg = new T();
                Marshal.PtrToStructure(handle.AddrOfPinnedObject(), msg);
                handle.Free();
                return msg;
            }

            return default(T);
        }
    }
}