module Test

open System
open Xunit
open TradSim.EventAggregation

[<Fact>]
let ``Order with Create Pending Event Received Resulting To Pending``() =
    let id = Guid.NewGuid()
    let occured = DateTimeOffset.UtcNow
    let expected = {Id = id; Symbol = "TT"; Price= 10.0m; Quantity = 11; TradedQuantity = 0; 
                    Direction= TradeDirection.Buy; UserId =1; Occured= occured ; 
                    Status= OrderStatus.Pending; Trades= List.empty}
    let accepted = OrderAccepted(id, "TT", 10.0m, 11, TradeDirection.Buy, occured, 1 )
    let actual = seq { yield accepted } |> aggregate

    Assert.Equal(expected, actual.Value)

[<Fact>]
let ``Order with Invalid Event Received on empty Order Resulting In Exception``() =
    let id = Guid.NewGuid()
    let occured = DateTimeOffset.UtcNow
    let canceled = OrderCanceled(id, occured, 1 )

    let exc = Assert.Throws<ArgumentException>(fun () -> seq { yield canceled } |> aggregate |> ignore)
    Assert.Equal("unknown event",exc.Message)

[<Fact>] 
let ``Order with Create Pending and Traded Event Received Resulting To PartiallyFilled``() =
    let id = Guid.NewGuid()
    let occured = DateTimeOffset.UtcNow
    let trade = {OrderId=id;Price= 10.0m;Quantity= 3;Occured= occured}
    let expected = {Id = id; Symbol = "TT"; Price= 10.0m; Quantity = 11; TradedQuantity = 3; 
                    Direction= TradeDirection.Buy; UserId =1; Occured= occured ; Status= OrderStatus.PartiallyFilled
                    Trades= [trade] }
    let accepted = OrderAccepted(id, "TT", 10.0m, 11, TradeDirection.Buy, occured, 1 )
    let traded = OrderTraded(trade.OrderId, trade.Price, trade.Quantity, trade.Occured, 1 )
    let actual = seq { yield accepted; yield traded } |> aggregate

    Assert.Equal(expected, actual.Value)

[<Fact>]
let ``Order with Create Pending and Traded Events Received Resulting To FullyFilled``() =
    let id = Guid.NewGuid()
    let occured = DateTimeOffset.UtcNow
    let trade1 = {OrderId=id;Price= 10.0m;Quantity= 3;Occured= DateTimeOffset.UtcNow}
    let trade2 = {OrderId=id;Price= 10.0m;Quantity= 8;Occured= occured}
    let expected = {Id = id; Symbol = "TT"; Price= 10.0m; Quantity = 11; TradedQuantity = 11; 
                    Direction= TradeDirection.Buy; UserId =1; Occured= occured ; 
                    Status= OrderStatus.FullyFilled; Trades= [trade1;trade2]}
    let accepted = OrderAccepted(id, "TT", 10.0m, 11, TradeDirection.Buy, DateTimeOffset.UtcNow, 1 )
    let traded1 = OrderTraded(trade1.OrderId, trade1.Price, trade1.Quantity, trade1.Occured, 1 )
    let traded2 = OrderTraded(trade2.OrderId, trade2.Price, trade2.Quantity, trade2.Occured, 1 )
    let actual = seq { yield accepted; yield traded1; yield traded2 } |> aggregate

    Assert.Equal(expected.TradedQuantity, actual.Value.TradedQuantity)
    Assert.Equal(expected.Occured, actual.Value.Occured)
    Assert.Equal(expected.Trades.Length, actual.Value.Trades.Length)

    for i = 0 to 1 do
        expected.Trades.Item  i = actual.Value.Trades.Item i |> ignore

[<Fact>]
let ``Order with Create Pending and Traded Events Received Resulting To OverFilled``() =
    let id = Guid.NewGuid()
    let occured = DateTimeOffset.UtcNow
    let trade1 = {OrderId=id;Price= 10.0m;Quantity= 3;Occured= DateTimeOffset.UtcNow}
    let trade2 = {OrderId=id;Price= 10.0m;Quantity= 9;Occured= occured}
    let expected = {Id = id; Symbol = "TT"; Price= 10.0m; Quantity = 11; TradedQuantity = 12; 
                    Direction= TradeDirection.Buy; UserId =1; Occured= occured ; 
                    Status= OrderStatus.FullyFilled; Trades= [trade1;trade2]}
    let accepted = OrderAccepted(id, "TT", 10.0m, 11, TradeDirection.Buy, DateTimeOffset.UtcNow, 1 )
    let traded1 = OrderTraded(trade1.OrderId, trade1.Price, trade1.Quantity, trade1.Occured, 1 )
    let traded2 = OrderTraded(trade2.OrderId, trade2.Price, trade2.Quantity, trade2.Occured, 1 )
    let actual = seq { yield accepted; yield traded1; yield traded2 } |> aggregate

    Assert.Equal(expected.TradedQuantity, actual.Value.TradedQuantity)
    Assert.Equal(expected.Occured, actual.Value.Occured)
    Assert.Equal(expected.Trades.Length, actual.Value.Trades.Length)

    for i = 0 to 1 do
         expected.Trades.Item i = actual.Value.Trades.Item i |> ignore