### About this project

## Overview
- Demonstrates how to implement the Saga pattern in .NET Core using the MassTransit library.
- Covers two types of sagas: Choreography and Orchestration, which resolve distributed transaction challenges in a microservices architecture.

## Versions
- **.NET Target Framework:** .NET 8.0
- **MassTransit:** Version 8.4.1

## Choreography sequence diagram

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
    Inventory->>Broker: Publish: InventoryDeductedEvent
    deactivate Inventory

    Broker-->>Delivery: Consume: InventoryDeductedEvent
    activate Delivery
    
    alt Happy Path (Delivery Feasible)

            Delivery->>Delivery: Verify Address & Dispatch Driver
            Delivery->>Broker: Publish: DeliveryStartedEvent (or Success)
            
            Broker-->>Order: Consume: DeliveryStartedEvent
            activate Order
            Order->>Order: Update DB (Status: Completed)
            deactivate Order

    else Sad Path (Invalid Address / No Driver / ...)
            Delivery-->>Delivery: Business Logic Error (e.g., Invalid Address)
            Delivery->>Broker: Publish: DeliveryFailedEvent
            deactivate Delivery
            
            Note over Broker, Order: START COMPENSATING TRANSACTIONS
            
            Broker-->>Inventory: Consume: DeliveryFailedEvent
            activate Inventory
            Inventory->>Inventory: Restore Stock Count
            Inventory->>Broker: Publish: InventoryRestoredEvent
            deactivate Inventory
            
            Broker-->>Order: Consume: InventoryRestoredEvent
            activate Order
            Order->>Order: Update DB (Status: Cancelled)
            deactivate Order
    end
```


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


