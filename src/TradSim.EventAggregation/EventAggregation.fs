module TradSim.EventAggregation

open System

type TradeDirection = Buy = 0 | Sell = 1
type OrderStatus = Pending = 0 | PartiallyFilled = 1 | FullyFilled = 2 | OverFilled = 3 | Canceled = 4

type Trade = {
    OrderId: Guid
    Price: decimal 
    Quantity: uint32
    Occured:DateTimeOffset
}

type Order = { 
    Id: Guid
    Symbol: string
    Price: decimal 
    Quantity: uint32
    TradedQuantity : uint32
    Direction: TradeDirection
    UserId :int
    Occured: DateTimeOffset
    Status: OrderStatus
    Trades: Trade list
}

type OrderEvent = 
    | OrderAccepted of OrderId: Guid * Symbol: string * Price: decimal * Quantity:uint32 * Direction: TradeDirection * Occured:DateTimeOffset * Version: uint32
    | OrderAmended of OrderId: Guid * Quantity: uint32 * Occured:DateTimeOffset * Version: uint32
    | OrderCancelled of OrderId: Guid * Occured:DateTimeOffset * Version: uint32
    | OrderTraded of OrderId: Guid * Price: decimal * Quantity:uint32 * Occured:DateTimeOffset * Version: uint32

let setOrderStatus quantity tradedQuantity =
    if  tradedQuantity = 0u then OrderStatus.Pending
    elif tradedQuantity < quantity then OrderStatus.PartiallyFilled
    elif tradedQuantity = quantity then OrderStatus.FullyFilled
    else OrderStatus.OverFilled

let createOrder orderId symbol price quantity direction occured : Order =
    { Id = orderId; Symbol = symbol; Price = price; Quantity = quantity; TradedQuantity = 0u; 
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
        | None, OrderAccepted(orderId,symbol,price,quantity,direction,occured,version) -> createOrder orderId symbol price quantity direction occured
        | None, _                                                                      -> raise (ArgumentException("unknown event"))
        | Some(order), OrderAmended(orderId, quantity, occured, version)               -> amendOrder order occured quantity
        | Some(order), OrderCancelled(orderId,occured,version)                         -> cancelOrder order occured
        | Some(order), OrderTraded(orderId,price,quantity,occured,version) 
                    -> applyTrade order orderId price quantity occured     
        | Some(order), _                                                               -> order

let aggregate events = 
    events |> Seq.fold (fun ord e -> Some(apply ord e)) None