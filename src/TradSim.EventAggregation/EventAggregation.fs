module TradSim.EventAggregation

open System

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
    | OrderCreated of OrderID: Guid * Symbol: string * Price: decimal * Quantity:int * Direction: TradeDirection * Occured:DateTimeOffset * Version: int
    | OrderAmended of OrderID: Guid * Quantity: int * Occured:DateTimeOffset * Version: int
    | OrderCanceled of OrderID: Guid * Occured:DateTimeOffset * Version: int
    | OrderTraded of OrderID: Guid * Price: decimal * Quantity:int * Occured:DateTimeOffset * Version: int

let apply item event = 
    match item, event with
        | None,    OrderCreated(orderID,symbol,price,quantity,direction,occured,version)  -> { Id = orderID; Symbol = symbol; Price = price; Quantity = quantity; TradedQuantity = 0; Direction = direction; UserId = 1; Occured= occured; Status= OrderStatus.Pending; }
        | None,    _                                                                      -> raise (ArgumentException("unknown event"))
        | Some(i), OrderAmended(orderID, quantity, occured, version)                      -> { i with Occured = occured; Quantity = i.Quantity + quantity; Status = setOrderStatus (i.Quantity + quantity) i.TradedQuantity }
        | Some(i), OrderCanceled(orderID,occured,version)                                 -> { i with Occured = occured; Status = OrderStatus.Cancelled }
        | Some(i), OrderTraded(orderId,price,quantity,occured,version)                    -> { i with Occured = occured; TradedQuantity = i.TradedQuantity + quantity; Status = setOrderStatus i.Quantity (i.TradedQuantity + quantity) }        
        | Some(i), _                                                                      -> i


let aggregate events = 
    events |> Seq.fold (fun acc e -> Some(apply acc e)) None