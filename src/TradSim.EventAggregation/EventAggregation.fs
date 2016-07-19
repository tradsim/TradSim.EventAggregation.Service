module TradSim.EventAggregation

open Newtonsoft.Json

type TradeDirection = Buy = 0 | Sell = 1
type OrderStatus = Pending = 0 | PartiallyFilled =1 | FullyFilled = 2 | OverFilled =3

type Order = { 
    Id: System.Guid
    Symbol: string
    Quantity: int
    Direction: TradeDirection
    UserId :int
    Occured: System.DateTimeOffset
    Status: OrderStatus
}

let getJsonNetJson value = 
    sprintf "I used to be %s but now I'm %s!" value  (JsonConvert.SerializeObject(value))