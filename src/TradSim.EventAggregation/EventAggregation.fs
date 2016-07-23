module TradSim.EventAggregation

open System

type TradeDirection = Buy = 0 | Sell = 1
type OrderStatus = Pending = 0 | PartiallyFilled = 1 | FullyFilled = 2 | OverFilled = 3 | Canceled = 4

type Trade = {
    OrderId: Guid
    Price: decimal 
    Quantity: int
    Occured:DateTimeOffset
}

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
    Trades: Trade list
}

type Event = 
    | OrderCreated of OrderId: Guid * Symbol: string * Price: decimal * Quantity:int * Direction: TradeDirection * Occured:DateTimeOffset * Version: int
    | OrderAmended of OrderId: Guid * Quantity: int * Occured:DateTimeOffset * Version: int
    | OrderCanceled of OrderId: Guid * Occured:DateTimeOffset * Version: int
    | OrderTraded of OrderId: Guid * Price: decimal * Quantity:int * Occured:DateTimeOffset * Version: int

let setOrderStatus quantity tradedQuantity =
    if  tradedQuantity = 0 then OrderStatus.Pending
    elif tradedQuantity < quantity then OrderStatus.PartiallyFilled
    elif tradedQuantity = quantity then OrderStatus.FullyFilled
    else OrderStatus.OverFilled

let createOrder orderId symbol price quantity direction occured : Order =
    { Id = orderId; Symbol = symbol; Price = price; Quantity = quantity; TradedQuantity = 0; 
      Direction = direction; UserId = 1; Occured= occured; Status= OrderStatus.Pending; Trades= [] }

let amendOrder order occured amendQuantity : Order =
    { order with Occured = occured; Quantity = order.Quantity + amendQuantity; 
                 Status = setOrderStatus (order.Quantity + amendQuantity) order.TradedQuantity}

let cancelOrder order occured =
    { order with Occured = occured; Status = OrderStatus.Canceled}

let applyTrade order orderId price quantity occured  = 
    { order with Occured = occured; TradedQuantity = order.TradedQuantity + quantity; 
                 Status = setOrderStatus order.Quantity (order.TradedQuantity + quantity); 
                 Trades = { OrderId= orderId; Price =price;Quantity = quantity; Occured = occured} :: order.Trades }

let apply item event = 
    match item, event with
        | None, OrderCreated(orderId,symbol,price,quantity,direction,occured,version)  -> createOrder orderId symbol price quantity direction occured
        | None, _                                                                      -> raise (ArgumentException("unknown event"))
        | Some(order), OrderAmended(orderId, quantity, occured, version)               -> amendOrder order occured quantity
        | Some(order), OrderCanceled(orderId,occured,version)                          -> cancelOrder order occured
        | Some(order), OrderTraded(orderId,price,quantity,occured,version)             -> applyTrade order orderId price quantity occured     
        | Some(order), _                                                               -> order

let aggregate events = 
    events |> Seq.fold (fun ord e -> Some(apply ord e)) None