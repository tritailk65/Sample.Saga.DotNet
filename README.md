### About this project

## Overview
- Demonstrates how to implement the Saga pattern in .NET Core using MassTransit library.
- Covers two types of sagas: Choreography and Orchestration, which resolve distributed transaction challenges in a microservices architecture.

## Versions
- **.NET Target Framework:** .NET 8.0
- **MassTransit:** Version 8.4.1

## How to run 
1. Clone this repository and navigate to the project directory:
```bash
git clone https://github.com/tritailk65/Sample.Saga.DotNet.git
cd Sample.Saga.Dotnet
```

2. Open the project with Visual Studio, Visual Studio Code, or your preferred IDE.

Start Docker Desktop (required for integration tests with a real database), then run:

```script
cd tests/Choreography.Integration.Tests
docker compose up -d
```

3. Build the project and run the tests:
```script
dotnet build
dotnet test
```

## Choreography flow (sequence diagram)

```mermaid
sequenceDiagram
    autonumber
    actor Customer
    participant Order as Order Service
    participant Broker as Message Broker
    participant Inventory as Inventory Service
    participant Delivery as Delivery Service

    %% Initial Flow
    Customer->>Order: Place Order
    activate Order
    Order->>Order: Save to DB (Status: Preparing)
    Order->>Broker: Publish: OrderCreatedEvent
    deactivate Order

    Broker-->>Inventory: Consume: OrderCreatedEvent
    activate Inventory
    Inventory->>Inventory: Check & Deduct Stock
    Inventory->>Broker: Publish: InventoryGoodsBookedEvent
    deactivate Inventory

    Broker-->>Delivery: Consume: InventoryGoodsBookedEvent
    activate Delivery
    
    alt Happy Path (Delivery Feasible)

            Delivery->>Delivery: Verify Address & Dispatch Driver
            Delivery->>Broker: Publish: DeliverySendSuccessEvent
            
            Broker-->>Order: Consume: DeliverySendSuccessEvent
            activate Order
            Order->>Order: Update DB (Status: Completed)
            deactivate Order

    else Sad Path (Invalid Address / No Driver / ...)
            Delivery-->>Delivery: Business Logic Error (e.g., Invalid Address)
            Delivery->>Broker: Publish: DeliverySendFailedEvent
            deactivate Delivery
            
            Note over Broker, Order: START COMPENSATING TRANSACTIONS
            
            Broker-->>Inventory: Consume: DeliverySendFailedEvent
            activate Inventory
            Inventory->>Inventory: Restore Stock Count
            %% SỬA Ở ĐÂY: Inventory publish event của chính nó
            Inventory->>Broker: Publish: InventoryRestoredEvent 
            deactivate Inventory
            
            %% SỬA Ở ĐÂY: Order đợi Inventory khôi phục xong mới Hủy
            Broker-->>Order: Consume: InventoryRestoredEvent
            activate Order
            Order->>Order: Update DB (Status: Cancelled)
            deactivate Order
    end
```

## Orchestration flow (sequence diagram)
```mermaid
sequenceDiagram
    autonumber

    participant Saga as OrderSaga (State Machine)
    
    participant OS as Order Service
    participant IS as Inventory Service
    participant DS as Delivery Service

    Note over Saga: State: Initial

    OS-->>Saga: [Event] OrderCreateEvent
    Saga->>Saga: InitializeSaga (Copy data)
    Note over Saga: State: OrderCreated

    alt Happy Path (Order creation successful)
        OS-->>Saga: [Event] OrderCreateEventSuccess
        Saga-->>IS: [Command] InventoryGoodsBookedInWarehouseEvent
        Note over Saga: State: BookingGoodsInWarehouse

        alt Happy Path (Inventory deduction successful)
            IS-->>Saga: [Event] InventoryGoodsBookedInWarehouseEventSuccess
            Saga-->>DS: [Command] DeliverySendEvent
            Note over Saga: State: DeliverySend

            alt Happy Path (Delivery successful)
                DS-->>Saga: [Event] DeliverySendEventSuccess
                Note over Saga: State: Final (Completed)
                
            else Delivery Technical Error (Sad Path)
                DS-->>Saga: [Event] DeliverySendEventFailed
                Note right of Saga: Start compensating transactions
                Saga-->>IS: [Event] InventoryGoodsRestoredEvent (Restore inventory)
                Saga-->>OS: [Event] OrderCancelEvent (Cancel order)
                Note over Saga: State: Canceled
            end

        else Business Error (Out of stock - Business Sad Path)
            IS-->>Saga: [Event] InventoryGoodsBookedRejectedEvent
            Note over Saga: State: Rejected
            
        else Inventory Technical Error (DB Timeout - Tech Sad Path)
            IS-->>Saga: [Event] InventoryGoodsBookedInWarehouseEventFailed
            Saga-->>OS: [Event] OrderCancelEvent (Cancel order)
            Note over Saga: State: Canceled
        end

    else Order Creation Error
        OS-->>Saga: [Event] OrderCreateEventFailed
        Saga-->>OS: [Event] OrderCancelEvent
        Note over Saga: State: Canceled
    end

    opt Error during Compensation (Compensation Failure)
        Note over OS, Saga: In Canceled/Rejected state but Consumer encounters Code/DB error
        OS--xSaga: [Event] Fault<OrderCancelEvent>
        Note right of Saga: LogCritical to alert Admin
        Note over Saga: State: Failed
    end
```



