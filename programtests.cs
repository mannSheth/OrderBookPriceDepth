using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Reflection;

namespace cs
{
    //Added some basic tests to make sure that the logic of my orderbook makes sense,

    [TestFixture]
    public class ProgramTests
    {
        [Test]
        public void MainNoArgsGivenPrintsError()
        { 
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);
            var args = new string[0];

            Program.Main(args);

            Assert.That(consoleOutput.ToString, Contains.Substring("No arguments given for this program please give a price depth"));
        }

        [Test]
        public void Main_InvalidArgsGiven_PrintsErrorMessage()
        {
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            var args = new string[] { "invalid" };

            Program.Main(args);

            Assert.That(consoleOutput.ToString(), Contains.Substring("Price depth argument is invalid. Please provide a positive integer value."));
        }
    }

    [TestFixture]
    public class OrderBookTests
    {
        private T GetPrivateField<T>(string fieldName, object instance)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)field.GetValue(instance);
        }

        private OrderBook _orderBook;

        [SetUp]
        public void Setup()
        {
            _orderBook = new OrderBook("BTC/USD", 5);
        }

        [Test]
        public void Add_OrderToBids_ShouldAddOrderToBids()
        {
            var order = new Order(1, "BTC/USD", OrderSide.Buy, 60000, 1);

            _orderBook.Add(order, 1);

            var bids = GetPrivateField<Dictionary<long, Order>>("_bids", _orderBook);
            Assert.That(bids.Count, Is.EqualTo(1));
            Assert.That(bids.ContainsKey(1), Is.True);
            Assert.That(bids[1].Price, Is.EqualTo(60000));
            Assert.That(bids[1].Quantity, Is.EqualTo(1));
        }

        [Test]
        public void Add_OrderToAsks_ShouldAddOrderToAsks()
        {
            var order = new Order(1, "BTC/USD", OrderSide.Sell, 61000, 1);

            _orderBook.Add(order, 1);

            var asks = GetPrivateField<Dictionary<long, Order>>("_asks", _orderBook);
            Assert.That(asks.Count, Is.EqualTo(1));
            Assert.That(asks.ContainsKey(1), Is.True);
            Assert.That(asks[1].Price, Is.EqualTo(61000));
            Assert.That(asks[1].Quantity, Is.EqualTo(1));
        }

        [Test]
        public void Add_OrderWithWrongSymbol_ShouldThrowArgumentException()
        {
            var order = new Order(1, "ETH/USD", OrderSide.Buy, 60000, 1);

            Assert.That(() => _orderBook.Add(order, 1), Throws.ArgumentException
                .With.Message.EqualTo("Order with id 1 already exists in the order book for symbol BTC/USD."));
        }

        [Test]
        public void Add_OrderWithZeroQuantity_ShouldThrowArgumentException()
        {
            var order = new Order(1, "BTC/USD", OrderSide.Buy, 60000, 0);

            Assert.That(() => _orderBook.Add(order, 1), Throws.ArgumentException
                .With.Message.EqualTo($"Invalid volume {order.Quantity} for order with id {order.Id}."));
        }

        [Test]
        public void Update_OrderInBids_ShouldUpdateOrderInBids()
        {
            var order = new Order(1, "BTC/USD", OrderSide.Buy, 60000, 1);
            _orderBook.Add(order, 1);
            var updatedOrder = new Order(1, "BTC/USD", OrderSide.Buy, 61000, 2);

            _orderBook.Update(updatedOrder, 2);

            var bids = GetPrivateField<Dictionary<long, Order>>("_bids", _orderBook);

            Assert.That(bids.Count, Is.EqualTo(1));
            Assert.That(bids.ContainsKey(1), Is.True);
            Assert.That(bids[1].Price, Is.EqualTo(61000));
            Assert.That(bids[1].Quantity, Is.EqualTo(2));
        }

        [Test]
        public void Update_OrderInAsks_ShouldUpdateOrderInAsks()
        {
            var order = new Order(1, "BTC/USD", OrderSide.Sell, 60000, 1);
            _orderBook.Add(order, 1);
            var updatedOrder = new Order(1, "BTC/USD", OrderSide.Sell, 61000, 2);

            _orderBook.Update(updatedOrder, 2);

            var asks = GetPrivateField<Dictionary<long, Order>>("_asks", _orderBook);

            Assert.That(asks.Count, Is.EqualTo(1));
            Assert.That(asks.ContainsKey(1), Is.True);
            Assert.That(asks[1].Price, Is.EqualTo(61000));
            Assert.That(asks[1].Quantity, Is.EqualTo(2));
        }


        [Test]
        public void Add_MultipleOrders_ShouldAddOrdersCorrectly()
        {
            var orders = new List<Order>
            {
            new Order(1, "BTC/USD", OrderSide.Buy, 60000, 1),
            new Order(2, "BTC/USD", OrderSide.Buy, 60500, 2),
            new Order(3, "BTC/USD", OrderSide.Sell, 61000, 1),
            new Order(4, "BTC/USD", OrderSide.Sell, 62000, 3)
            };


            foreach (var order in orders)
            {
                _orderBook.Add(order, 1);
            }

            var bids = GetPrivateField<Dictionary<long, Order>>("_bids", _orderBook);
            var asks = GetPrivateField<Dictionary<long, Order>>("_asks", _orderBook);

            Assert.That(bids.Count, Is.EqualTo(2));
            Assert.That(asks.Count, Is.EqualTo(2));
            Assert.That(bids[1].Price, Is.EqualTo(60000));
            Assert.That(bids[1].Quantity, Is.EqualTo(1));
            Assert.That(bids[2].Price, Is.EqualTo(60500));
            Assert.That(bids[2].Quantity, Is.EqualTo(2));
            Assert.That(asks[3].Price, Is.EqualTo(61000));
            Assert.That(asks[3].Quantity, Is.EqualTo(1));
            Assert.That(asks[4].Price, Is.EqualTo(62000));
            Assert.That(asks[4].Quantity, Is.EqualTo(3));

        }

        [Test]
        public void Execute_ThrowsArgumentException_WhenOrderSymbolDoesNotMatchOrderBookSymbol()
        {
            var order = new Order(1, "TSLA", OrderSide.Buy, 100, 1000);

            Assert.Throws<ArgumentException>(() => _orderBook.Execute(order, 0));
        }

        [Test]
        public void Execute_ThrowsArgumentException_WhenOrderDoesNotExistInOrderBook()
        {
            var order1 = new Order(1, "BTC/USD", OrderSide.Buy, 100, 1000);
            var order2 = new Order(2, "BTC/USD", OrderSide.Sell, 200, 2000);
            _orderBook.Add(order1, 0);

            Assert.Throws<ArgumentException>(() => _orderBook.Execute(order2, 0));
        }

        [Test]
        public void Execute_RemovesOrderQuantityFromOrderBook()
        {
            var order = new Order(1, "BTC/USD", OrderSide.Buy, 100, 1000);
            _orderBook.Add(order, 0);

            _orderBook.Execute(order, 0);
            var bids = GetPrivateField<Dictionary<long, Order>>("_bids", _orderBook);
            
            Assert.AreEqual(0, bids.Count);
        }

        [Test]
        public void Execute_UpdatesPriceDepth_WhenOrderQuantityIsNotZero()
        {
            var order1 = new Order(1, "BTC/USD", OrderSide.Buy, 100, 1000);
            var order2 = new Order(2, "BTC/USD", OrderSide.Buy, 50, 950);
            _orderBook.Add(order1, 0);
            _orderBook.Add(order2, 0);

            _orderBook.Execute(new Order(2, "BTC/USD", OrderSide.Buy, default, 400), 0);
            var bids = GetPrivateField<Dictionary<long, Order>>("_bids", _orderBook);
            
            Assert.AreEqual(2, bids.Count);
            Assert.AreEqual(550, bids[2].Quantity);
        }

        //should've added some price depth tests aswell.
    }
}
