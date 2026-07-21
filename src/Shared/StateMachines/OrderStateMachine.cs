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
    public State Canceled { get; private set; } = null!;
    public State Rejected { get; private set; } = null!;

    private Event<OrderCreateEvent> OnSagaStarted { get; set; } = null!;
    private Event<InventoryGoodsBookedInWarehouseEventSuccess> OnGoodsBookedInWarehouseSuccess { get; set; } = null!;
    private Event<InventoryGoodsBookedInWarehouseEventFailed> OnGoodsBookedInWarehouseFailed { get; set; } = null!;
    private Event<OrderCreateEventSuccess> OnOrderCreateSuccess { get; set; } = null!;
    private Event<OrderCreateEventFailed> OnOrderCreateFailed { get; set; } = null!;
    private Event<DeliverySendEventSuccess> OnDeliverySendEventSuccess { get; set; } = null!;
    private Event<DeliverySendEventFailed> OnDeliverySendEventFailed { get; set; } = null!;
    private Event<InventoryGoodsBookedRejectedEvent> OnGoodsBookedRejected { get; set;} = null!;

    // Fault 
    private Event<Fault<OrderCancelEvent>> OnCancelOrderFault { get; set;} = null!;
    private Event<Fault<InventoryGoodsBookedRejectedEvent>> OnRejectOrderFault {get; set;} = null!;

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
        Event(() => OnGoodsBookedRejected, context => context.CorrelateById(x => x.Message.OrderId));

        // Map correlation for fault state
        Event(() => OnCancelOrderFault, x => x.CorrelateById(context => context.Message.Message.OrderId));
        Event(() => OnRejectOrderFault, x => x.CorrelateById(context => context.Message.Message.OrderId));

        Initially(WhenSagaStarted());

        During(OrderCreated,
            When(OnOrderCreateSuccess)
                .Publish(context => new InventoryGoodsBookedInWarehouseEvent(context.Message.OrderId, context.Message.UserId, context.Message.CartItems, context.Message.Address))                
                .TransitionTo(BookingGoodsInWarehouse),
            When(OnOrderCreateFailed)
                .Publish(context => new OrderCancelEvent(context.Message.OrderId))
                .TransitionTo(Canceled)
        );

        During(BookingGoodsInWarehouse,
            When(OnGoodsBookedInWarehouseSuccess)
                .Publish(context => new DeliverySendEvent(context.Message.OrderId, context.Message.CartItems, context.Message.UserId, context.Message.Address))
                .TransitionTo(DeliverySend),
            When(OnGoodsBookedRejected)
                .TransitionTo(Rejected),
            When(OnGoodsBookedInWarehouseFailed)
                .Publish(context => new OrderCancelEvent(context.Message.OrderId))
                .TransitionTo(Canceled)    
        );

        During(DeliverySend,
            When(OnDeliverySendEventSuccess)
                .TransitionTo(Final),
            When(OnDeliverySendEventFailed)
                .Publish(context => new InventoryGoodsRestoredEvent(context.CorrelationId!.Value, context.Saga.Goods.ToDictionary(id => id.Id, model => model.Count)))
                .Publish(context => new OrderCancelEvent(context.Message.OrderId))
                .TransitionTo(Canceled)
        );

        During(Rejected, Canceled,
            When(OnRejectOrderFault)
                //Log critial error
                .Then(LogSagaState)
                .TransitionTo(Failed),
            When(OnCancelOrderFault)
                .Then(LogSagaState)
                .TransitionTo(Failed)
        );
        
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

    private void LogSagaState<TEvent>(BehaviorContext<OrderSaga, TEvent> context) where TEvent : class
    {
        _logger.LogCritical($"{nameof(OrderSaga)} | correlationId: {context.Saga.CorrelationId} | event: {context.Event.Name}");
        context.Saga.UpdateAt = DateTime.UtcNow;
    }

}
