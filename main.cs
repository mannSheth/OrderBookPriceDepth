using System.Collections.Concurrent;
using System.Text;

namespace cs
{
    //main method that gets the price depth from the user.
    //The binary stream that is piped to standard input is parsed and stored as orders and sent to the right orderbook.
    //Through this process, pricedepth is outputted everytime a change within the levels occurs.
    class Program
    {
        public static void Main(string[] args)
        {
            var s = Console.OpenStandardInput();
            Header header;
            object message;
            Dictionary<string, OrderBook> orderBooks = new Dictionary<string, OrderBook>();
            int priceDepth = 0;

            if (args.Length > 0)
            {
                if (int.TryParse(args[0], out int pd) && pd > 0)
                {
                    priceDepth = pd;
                }
                else
                {
                    Console.WriteLine("Price depth argument is invalid. Please provide a positive integer value.");
                    return;
                }
            }
            else
            {
                Console.WriteLine("No arguments given for this program please give a price depth");
                return;
            }

            while (((header, message) = Message.ReadNext(s)) != (null, null))
            {
                //Console.WriteLine($"{header}{message}");
                Order newOrder;

                switch (header.MsgType)
                {
                    case 'A':
                        var messageFormatA = (OrderAdd)message;
                        newOrder = new Order(messageFormatA.OrderId, messageFormatA.Symbol, (OrderSide)messageFormatA.Side, messageFormatA.Price, messageFormatA.Volume);
                        if (!orderBooks.ContainsKey(messageFormatA.Symbol))
                        {
                            orderBooks[messageFormatA.Symbol] = new OrderBook(messageFormatA.Symbol, priceDepth);
                        }
                        orderBooks[messageFormatA.Symbol].Add(newOrder, header.SeqNum);
                        break;
                    case 'E':
                        var messageFormatE = (OrderTrade)message;
                        newOrder = new Order(messageFormatE.OrderId, messageFormatE.Symbol, (OrderSide)messageFormatE.Side, default, messageFormatE.Volume);
                        orderBooks[messageFormatE.Symbol].Execute(newOrder, header.SeqNum);
                        break;
                    case 'D':
                        var messageFormatD = (OrderDelete)message;
                        newOrder = new Order(messageFormatD.OrderId, messageFormatD.Symbol, (OrderSide)messageFormatD.Side);
                        orderBooks[messageFormatD.Symbol].Delete(newOrder, header.SeqNum);
                        break;
                    case 'U':
                        var messageFormatU = (OrderUpdate)message;
                        newOrder = new Order(messageFormatU.OrderId, messageFormatU.Symbol, (OrderSide)messageFormatU.Side, messageFormatU.Price, messageFormatU.Volume);
                        orderBooks[messageFormatU.Symbol].Update(newOrder, header.SeqNum);
                        break;
                    default:
                        throw new ArgumentException($"unknown message type: {header.MsgType}");
                }
            }
        }

    }
}
