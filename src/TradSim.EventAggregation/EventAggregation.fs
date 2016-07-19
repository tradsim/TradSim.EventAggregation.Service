module TradSim.EventAggregation

open System
open Newtonsoft.Json

type TradeDirection = Buy = 0 | Sell = 1
type OrderStatus = Pending = 0 | PartiallyFilled = 1 | FullyFilled = 2 | OverFilled = 3

type Order = { 
    Id: Guid
    Symbol: string
    Quantity: int
    TradedQuantity : int
    Direction: TradeDirection
    UserId :int
    Occured: DateTimeOffset
    Status: OrderStatus
}

type Event = 
    | OrderCreatePending of OrderID: string * Symbol: string * Price: decimal * Quantity:int * Direction: TradeDirection * Occured:DateTimeOffset * Version: int
    | OrderCreated of OrderID: string * Symbol: string * Price: decimal * Quantity:int * Direction: TradeDirection * Occured:DateTimeOffset * Version: int
    | OrderAmendPending of OrderID: Guid * Occured:DateTimeOffset * Version: int
    | OrderAmended of OrderID: Guid * Occured:DateTimeOffset * Version: int
    | OrderCancelPending of OrderID: Guid * Occured:DateTimeOffset * Version: int
    | OrderCanceled of OrderID: Guid * Occured:DateTimeOffset * Version: int
    | OrderTraded of OrderID: string * Price: decimal * Quantity:int * Direction: TradeDirection * Occured:DateTimeOffset * Version: int

let getJsonNetJson value = 
    sprintf "I used to be %s but now I'm %s!" value  (JsonConvert.SerializeObject(value))