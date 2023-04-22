using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace cs
{
    //Basic design of how the orders are stored and processed.
    //The order book also keeps track of the pricedepth and outputs if it has been changed.

    public class PriceLevel
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }

    /// <summary>
    /// basic enum to track what side the order is going to go on.
    /// </summary>
    public enum OrderSide
    {
        Buy = 'B',
        Sell = 'S'
    }

    /// <summary>
    /// This class captures each order and stores the necessary information, vital information must always be 
    /// present and if an order type does not have a price or volume default values are given. e.g. DeleteOrder.
    /// </summary>
    public class Order
    {
        public Order(long id, string symbol, OrderSide side, decimal price = 0, decimal quantity = 0)
        {
            Id = id;
            Symbol = symbol;
            Side = side;
            Price = price;
            Quantity = quantity;
        }

        public long Id { get; set; }
        public string Symbol { get; set; }
        public OrderSide Side { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }

    public class OrderBook
    {
        private readonly string _symbol;
        private readonly Dictionary<long, Order> _asks;
        private readonly Dictionary<long, Order> _bids;
        private List<PriceLevel> _askDepth;
        private List<PriceLevel> _bidDepth;
        private readonly int _priceDepthLimit;

        public OrderBook(string symbol, int priceDepthLimit)
        {
            _symbol = symbol;
            _asks = new Dictionary<long, Order>();
            _bids = new Dictionary<long, Order>();
            _bidDepth = new List<PriceLevel>();
            _askDepth = new List<PriceLevel>();
            _priceDepthLimit = priceDepthLimit;
        }

        /// <summary>
        /// Adds a order to either the bids or asks dicitonary with the given id, price and quantity.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="sNum"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Add(Order order, int sNum)
        {
            if (order.Symbol != _symbol)
            {
                throw new ArgumentException($"Order with id {order.Id} already exists in the order book for symbol {_symbol}.");
            }

            if (order.Quantity <= 0)
            {
                throw new ArgumentException($"Invalid volume {order.Quantity} for order with id {order.Id}.");
            }
            var orderSide = order.Side == OrderSide.Buy ? _bids : _asks;
            orderSide.Add(order.Id, order);
            UpdatePriceDepth(sNum);
        }

        /// <summary>
        /// Updates a given order through id and order-side.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="sNum"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Update(Order order, int sNum)
        {
            if (order.Symbol != _symbol)
            {
                throw new ArgumentException($"Order symbol ({order.Symbol}) does not match the OrderBook's symbol ({_symbol}).");
            }

            var orderSide = order.Side == OrderSide.Buy ? _bids : _asks;

            if (!orderSide.ContainsKey(order.Id))
            {
                throw new ArgumentException($"Order with id {order.Id} does not exist in the order book for symbol {_symbol}.");
            }

            if (order.Quantity <= 0)
            {
                throw new ArgumentException($"Invalid volume {order.Quantity} for order with id {order.Id}.");
            }

            orderSide[order.Id] = order;
            UpdatePriceDepth(sNum);
        }

        /// <summary>
        /// Removes the quantity from existing orders this is done using the id and the order-side
        /// </summary>
        /// <param name="order"></param>
        /// <param name="sNum"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Execute(Order order, int sNum)
        {
            if (order.Symbol != _symbol)
            {
                throw new ArgumentException($"Order symbol ({order.Symbol}) does not match the OrderBook's symbol ({_symbol}).");
            }

            var orderSide = order.Side == OrderSide.Buy ? _bids : _asks;

            if (!orderSide.ContainsKey(order.Id))
            {
                throw new ArgumentException($"Order with id {order.Id} does not exist in the order book for symbol {_symbol}.");
            }

            orderSide[order.Id].Quantity -= order.Quantity;

            if (orderSide[order.Id].Quantity <= 0)
            {
                Delete(order, sNum);
            }
            else
            {
                UpdatePriceDepth(sNum);
            }
        }

        /// <summary>
        /// Delets and an order using the id and order side.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="sNum"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Delete(Order order, int sNum)
        {
            if (order.Symbol != _symbol)
            {
                throw new ArgumentException($"Order symbol ({order.Symbol}) does not match the OrderBook's symbol ({_symbol}).");
            }

            var orderSide = order.Side == OrderSide.Buy ? _bids : _asks;

            if (!orderSide.ContainsKey(order.Id))
            {
                throw new ArgumentException($"Order with id {order.Id} does not exist in the order book for symbol {_symbol}.");
            }

            orderSide.Remove(order.Id);
            UpdatePriceDepth(sNum);
        }

        /// <summary>
        /// This is called after every order to see if the price depth snapshot has changes from the given order.
        /// If so then the snapshot is updated and the new pricedepth for this symbol is outputted.
        /// </summary>
        /// <param name="sNum"></param>
        private void UpdatePriceDepth(int sNum)
        {
            //this is kinda ugly ngl
            var askGroupedOrders = _asks.Values.GroupBy(o => o.Price)
                                               .Select(g => new PriceLevel { Price = g.Key, Quantity = g.Sum(o => o.Quantity) });
            var bidGroupedOrders = _bids.Values.GroupBy(o => o.Price)
                                               .Select(g => new PriceLevel { Price = g.Key, Quantity = g.Sum(o => o.Quantity) });

            var askOrders = askGroupedOrders.OrderBy(o => o.Price)
                                           .Take(_priceDepthLimit)
                                           .ToList();
            var bidOrders = bidGroupedOrders.OrderByDescending(o => o.Price)
                                           .Take(_priceDepthLimit)
                                           .ToList();

            if (!IsSamePriceLevels(askOrders, _askDepth) || !IsSamePriceLevels(bidOrders, _bidDepth))//global list but still passing through idk most probs a bad idea.
            {
                _askDepth = askOrders;
                _bidDepth = bidOrders;
                PrintPriceDepths(sNum);
            }
        }

        /// <summary>
        /// checks the snapshot to the new sequence after the order got processed.
        /// and returns true or false depending on the whether the sequences match.
        /// </summary>
        /// <param name="newLevels"></param>
        /// <param name="oldLevels"></param>
        /// <returns></returns>
        private bool IsSamePriceLevels(List<PriceLevel> newLevels, List<PriceLevel> oldLevels)//
        {
            if (newLevels.Count != oldLevels.Count)
            {
                return false;
            }

            for (int i = 0; i < newLevels.Count; i++)
            {
                if (newLevels[i].Price != oldLevels[i].Price || newLevels[i].Quantity != oldLevels[i].Quantity)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// outputs the asks and bids pricedepth for given 'n' if it has changed.
        /// </summary>
        /// <param name="sNum"></param>
        private void PrintPriceDepths(int sNum)
        {
            var symbol = _symbol;
            var bids = _bidDepth.Select(x => (x.Price, x.Quantity)).ToList();
            var asks = _askDepth.Select(x => (x.Price, x.Quantity)).ToList();

            var sb = new StringBuilder();
            sb.Append($"{sNum}, {symbol}, ");
            sb.Append("[");
            sb.Append(string.Join(", ", bids.Select(x => $"({x.Price}, {x.Quantity})")));
            sb.Append("], ");
            sb.Append("[");
            sb.Append(string.Join(", ", asks.Select(x => $"({x.Price}, {x.Quantity})")));
            sb.Append("]");

            var orderBookString = sb.ToString();
            Console.WriteLine(orderBookString);
        }

    }
}
