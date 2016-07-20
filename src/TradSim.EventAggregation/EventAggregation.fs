module TradSim.EventAggregation

open System
open Newtonsoft.Json

type TradeDirection = Buy = 0 | Sell = 1
type OrderStatus = Pending = 0 | PartiallyFilled = 1 | FullyFilled = 2 | OverFilled = 3 | Cancelled = 4

let setOrderStatus quantity tradedQuantity =
    if  tradedQuantity = 0 then OrderStatus.Pending
    elif tradedQuantity < quantity then OrderStatus.PartiallyFilled
    elif tradedQuantity = quantity then OrderStatus.FullyFilled
    else OrderStatus.OverFilled     

type Order = { 
    Id: Guid
    Symbol: string
    Price: decimal 
    Quantity: int
    TradedQuantity : int
    Direction: TradeDirection
    UserId :int
    Occured: DateTimeOffset
    Status: OrderStatus
}

type Event = 
    | OrderCreatePending of OrderID: Guid * Symbol: string * Price: decimal * Quantity:int * Direction: TradeDirection * Occured:DateTimeOffset * Version: int
    | OrderCreated of OrderID: Guid * Occured:DateTimeOffset * Version: int
    | OrderAmendPending of OrderID: Guid * Occured:DateTimeOffset * Version: int
    | OrderAmended of OrderID: Guid * Occured:DateTimeOffset * Version: int
    | OrderCancelPending of OrderID: Guid * Occured:DateTimeOffset * Version: int
    | OrderCanceled of OrderID: Guid * Occured:DateTimeOffset * Version: int
    | OrderTraded of OrderID: Guid * Price: decimal * Quantity:int * Direction: TradeDirection * Occured:DateTimeOffset * Version: int

let apply item event = 
    match item, event with
        | None,    OrderCreatePending(orderID,symbol,price,quantity,direction,occured,version)  -> { Id = orderID; Symbol = symbol; Price = price; Quantity = quantity; TradedQuantity = 0; Direction = direction; UserId = 1; Occured= occured; Status= OrderStatus.Pending; }
        | None,    _                                                                            -> raise (ArgumentException("unknown event"))
        | Some(i), OrderCreated(orderID,occured,version)                                        -> { i with Occured = occured; }        
        | Some(i), OrderAmendPending(orderID,occured,version)                                   -> { i with Occured = occured; }
        | Some(i), OrderAmended(orderID,occured,version)                                        -> { i with Occured = occured; }
        | Some(i), OrderCancelPending(orderID,occured,version)                                  -> { i with Occured = occured; }
        | Some(i), OrderCanceled(orderID,occured,version)                                       -> { i with Occured = occured; Status = OrderStatus.Cancelled }
        | Some(i), OrderTraded(orderId,price,quantity,direction,occured,version)                -> { i with Occured = occured; TradedQuantity = i.TradedQuantity + quantity; Status = setOrderStatus i.Quantity (i.TradedQuantity + quantity) }        
        | Some(i), _                                                                            -> i


let aggregate events = 
    events |> Seq.fold (fun acc e -> Some(apply acc e)) None

let getJsonNetJson value = 
    sprintf "I used to be %s but now I'm %s!" value  (JsonConvert.SerializeObject(value))