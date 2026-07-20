using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.StateMachines;

public sealed class OrderStateMachine : MassTransitStateMachine<OrderSaga>
{
    private readonly ILogger<OrderStateMachine> _logger;

    public State OrderCreated { get; private set; } = null!;
    public State BookingGoodsInWarehouse { get; private set; } = null!;
    public State DeliverySend { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    private Event<OrderCreateEvent> OnSagaStarted { get; set; } = null!;
    private Event<InventoryGoodsBookedInWarehouseEventSuccess> OnGoodsBookedInWarehouseSuccess { get; set; } = null!;
    private Event<InventoryGoodsBookedInWarehouseEventFailed> OnGoodsBookedInWarehouseFailed { get; set; } = null!;
    private Event<OrderCreateEventSuccess> OnOrderCreateSuccess { get; set; } = null!;
    private Event<OrderCreateEventFailed> OnOrderCreateFailed { get; set; } = null!;
    private Event<DeliverySendEventSuccess> OnDeliverySendEventSuccess { get; set; } = null!;
    private Event<DeliverySendEventFailed> OnDeliverySendEventFailed { get; set; } = null!;

    public OrderStateMachine(ILogger<OrderStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        Event(() => OnSagaStarted, context => context.CorrelateById(x => x.Message.OrderId));
        Event(() => OnGoodsBookedInWarehouseSuccess, context => context.CorrelateById(x => x.Message.OrderId));
        Event(() => OnGoodsBookedInWarehouseFailed, context => context.CorrelateById(x => x.Message.OrderId));
        Event(() => OnOrderCreateSuccess, context => context.CorrelateById(x => x.Message.OrderId));
        Event(() => OnOrderCreateFailed, context => context.CorrelateById(x => x.Message.OrderId));
        Event(() => OnDeliverySendEventSuccess, context => context.CorrelateById(x => x.Message.OrderId));
        Event(() => OnDeliverySendEventFailed, context => context.CorrelateById(x => x.Message.OrderId));

        Initially(WhenSagaStarted());

        During(OrderCreated,
            When(OnOrderCreateSuccess)
                .Publish(context => new InventoryGoodsBookedInWarehouseEvent(context.Message.OrderId, context.Message.UserId, context.Message.CartItems, context.Message.Address))                
                .TransitionTo(BookingGoodsInWarehouse),
            When(OnOrderCreateFailed)
                .Publish(context => new OrderCancelEvent(context.CorrelationId!.Value))
                .TransitionTo(Failed)
        );

        During(BookingGoodsInWarehouse,
            When(OnGoodsBookedInWarehouseSuccess)
                .Publish(context => new DeliverySendEvent(context.Message.OrderId, context.Message.CartItems, context.Message.UserId, context.Message.Address))
                .TransitionTo(DeliverySend),
            When(OnGoodsBookedInWarehouseFailed)
                .Publish(context => new OrderCancelEvent(context.CorrelationId!.Value))
                .TransitionTo(Failed)    
        );

        During(DeliverySend,
            When(OnDeliverySendEventSuccess)
                .TransitionTo(Final),
            When(OnDeliverySendEventFailed)
                .Publish(context => new InventoryGoodsRestoredEvent(context.CorrelationId!.Value, context.Saga.Goods.ToDictionary(id => id.Id, model => model.Count)))
                .Publish(context => new OrderCancelEvent(context.CorrelationId!.Value))
                .TransitionTo(Failed)
        );

        // DuringAny(
        //     When(OnDeliverySendEventFailed)
        //         .Publish(context => new InventoryGoodsRestoredEvent(context.CorrelationId!.Value, context.Saga.Goods.ToDictionary(id => id.Id, model => model.Count)))
        //         .Publish(context => new OrderCancelEvent(context.CorrelationId!.Value))
        //         .TransitionTo(Failed)
        // );
        
    }

    private EventActivityBinder<OrderSaga, OrderCreateEvent> WhenSagaStarted()
    {
        return When(OnSagaStarted)
            .Then(InitializeSaga)
            .TransitionTo(OrderCreated);
    }

    private void InitializeSaga(BehaviorContext<OrderSaga, OrderCreateEvent> context)
    {
        context.Saga.Goods = context.Message.CartItems;
        context.Saga.DeliveryAddress = context.Message.Address;
        context.Saga.CorrelationId = context.Message.OrderId;
        context.Saga.UserId = context.Message.UserId;
        context.Saga.RequestId = context.RequestId;
        context.Saga.ResponseAddress = context.ResponseAddress;
        context.Saga.CreatedAt = DateTime.UtcNow;
    }

}

    // public static class OrderStateMachineExtension
    // {
    //     public static EventActivityBinder<OrderSaga, OrderCreateEvent> CopyDataToInstance(this EventActivityBinder<OrderSaga, OrderCreateEvent> binder)
    //     {
    //         return binder.Then(x =>
    //         {
    //             x.Saga.Goods = x.Message.CartItems;
    //             x.Saga.DeliveryAddress = x.Message.Address;
    //             x.Saga.CorrelationId = x.Message.OrderId;
    //             x.Saga.UserId = x.Message.UserId;
    //             x.Saga.RequestId = x.RequestId;
    //             x.Saga.ResponseAddress = x.ResponseAddress;
    //             x.Saga.CreatedAt = DateTime.UtcNow;
    //         });
    //     }
    // }