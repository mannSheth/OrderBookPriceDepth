# Orderbook & Price Depth

Implementation of a simple order book that generates price depth snapshots while processing a binary stream of order data.

## Build

Building with Visual Studio

 - you can open the csproj file and build it with 'Ctrl+Shift+B'

Building with the .NET CLI

 - Navigate to the project directory
 - Build the project using the .NET CLI: 
 
 ```
 dotnet build
 ```

## Usage

To run the program:
```CLI
dotnet [Navigate to the cs.dll] [Price depth] < [Input that needs to be parsed]
```
e.g.
```
dotnet bin\\Debug\\net6.0\\cs.dll 5 < input1.stream
```

## Input Format After Parsing Example

```
1/A/{'symbol': 'GME', 'order_id': 6990022307456631368, 'side': 'B', 'volume': 5000, 'price': 318800}
```

## Price Depth Format

```
sequenceNo, symbol, [(bidPrice1, bidVolume1), ...], [(askPrice1, askVolume1), ...]
```

## Scope

This program is a simple implementation of an order book and price depth generator, designed for an interview assessment. The implementation includes basic functionality to process a binary stream of order data and generate price depth snapshots. Please note that the program does not include more complex algorithms, such as sophisticated matching algorithms, and is not designed for multithreading.

The primary goals of this implementation are to showcase:

- The ability to parse a binary stream of order data and create an order book.
- The ability to generate price depth snapshots for the order book.
