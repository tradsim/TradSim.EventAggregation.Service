module Test

open System
open Xunit
open TradSim.EventAggregation

[<Fact>]
let ``Order with Create Pending Event Received Resulting To Pending``() =
    let id = Guid.NewGuid()
    let occured = DateTimeOffset.UtcNow
    let expected = {Id = id; Symbol = "TT"; Price= 10.0m; Quantity = 11; TradedQuantity = 0; Direction= TradeDirection.Buy; UserId =1; Occured= occured ; Status= OrderStatus.Pending}
    let created = OrderCreated(id, "TT", 10.0m, 11, TradeDirection.Buy, occured, 1 )
    let actual = seq { yield created } |> aggregate

    Assert.Equal(expected, actual.Value)

[<Fact>]
let ``Order with Invalid Event Received on empty Order Resulting In Exception``() =
    let id = Guid.NewGuid()
    let occured = DateTimeOffset.UtcNow
    let canceled = OrderCanceled(id, occured, 1 )
    let actual = seq { yield canceled } |> aggregate

    Assert.Throws<ArgumentException>(fun () -> actual.Value |> ignore)

[<Fact>]
let ``Order with Create Pending and Traded Event Received Resulting To PartiallyFilled``() =
    let id = Guid.NewGuid()
    let occured = DateTimeOffset.UtcNow
    let expected = {Id = id; Symbol = "TT"; Price= 10.0m; Quantity = 11; TradedQuantity = 3; Direction= TradeDirection.Buy; UserId =1; Occured= occured ; Status= OrderStatus.PartiallyFilled}
    let createPending = OrderCreated(id, "TT", 10.0m, 11, TradeDirection.Buy, occured, 1 )
    let traded = OrderTraded(id, 10.0m, 3, occured, 1 )
    let actual = seq { yield createPending; yield traded } |> aggregate

    Assert.Equal(expected, actual.Value)

[<Fact>]
let ``Order with Create Pending and Traded Events Received Resulting To FullyFilled``() =
    let id = Guid.NewGuid()
    let occured = DateTimeOffset.UtcNow
    let expected = {Id = id; Symbol = "TT"; Price= 10.0m; Quantity = 11; TradedQuantity = 11; Direction= TradeDirection.Buy; UserId =1; Occured= occured ; Status= OrderStatus.FullyFilled}
    let createPending = OrderCreated(id, "TT", 10.0m, 11, TradeDirection.Buy, DateTimeOffset.UtcNow, 1 )
    let traded = OrderTraded(id, 10.0m, 3, DateTimeOffset.UtcNow, 1 )
    let traded2 = OrderTraded(id, 10.0m, 8, occured, 1 )
    let actual = seq { yield createPending; yield traded; yield traded2 } |> aggregate

    Assert.Equal(expected, actual.Value)

[<Fact>]
let ``Order with Create Pending and Traded Events Received Resulting To OverFilled``() =
    let id = Guid.NewGuid()
    let occured = DateTimeOffset.UtcNow
    let expected = {Id = id; Symbol = "TT"; Price= 10.0m; Quantity = 11; TradedQuantity = 12; Direction= TradeDirection.Buy; UserId =1; Occured= occured ; Status= OrderStatus.OverFilled}
    let createPending = OrderCreated(id, "TT", 10.0m, 11, TradeDirection.Buy, DateTimeOffset.UtcNow, 1 )
    let traded = OrderTraded(id, 10.0m, 3, DateTimeOffset.UtcNow, 1 )
    let traded2 = OrderTraded(id, 10.0m, 9, occured, 1 )
    let actual = seq { yield createPending; yield traded; yield traded2 } |> aggregate

    Assert.Equal(expected, actual.Value)