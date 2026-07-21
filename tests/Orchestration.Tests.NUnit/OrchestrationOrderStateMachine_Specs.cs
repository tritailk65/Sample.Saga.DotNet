using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using Shared.StateMachines;

namespace Orchestration.Tests.NUnit;

[TestFixture]
public class When_order_is_added
{
    private ServiceProvider _provider;
    private ITestHarness _harness;
    private ISagaStateMachineTestHarness<OrderStateMachine, OrderSaga> _sagaHarness = null!;

    #region  Setup and TearDown
    [SetUp]
    public async Task Setup()
    {
        _provider = new ServiceCollection()
            .ConfigureMassTransit(x =>
            {
                x.AddSagaStateMachine<OrderStateMachine, OrderSaga>();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetTestHarness();
        await _harness.Start();

        _sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSaga>();
    }

    [TearDown]
    public async Task Teardown()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }
    #endregion

    #region Mock Data
    public static readonly Guid UserId = Guid.Parse("b185922e-3061-49a1-a9e6-28521eeca2f9");
    
    public static readonly GoodViewModel Good = new()
    {
        Id = Guid.Parse("cf7c1502-a22b-4d0a-8f95-5b802e2f7948"),
        Name = "Product",
        Count = 1,
        Price = 100,
    };
    #endregion

    #region State Machine Unit test
    [Test]
    public async Task Should_create_a_saga_instance()
    {
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";
        var orderId = NewId.NextGuid();

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));
        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), Is.True, "Message not consumed");

        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSaga>();

        Assert.That(await sagaHarness.Consumed.Any<OrderCreateEvent>(), "Message not consumed by saga");

        Assert.That(await sagaHarness.Created.Any(x => x.CorrelationId == orderId));

        var instance = sagaHarness.Created.ContainsInState(orderId, sagaHarness.StateMachine, sagaHarness.StateMachine.OrderCreated);
        Assert.That(instance, Is.Not.Null, "Saga instance not found");

        Guid? existsId = await sagaHarness.Exists(orderId, x => x.OrderCreated);
        Assert.That(existsId.HasValue, Is.True, "Saga did not exist");
    }

    [Test]
    public async Task Should_change_state_to_BookingGoodsInWarehouse_when_order_added_success()
    {
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";
        var orderId = NewId.NextGuid();

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));

        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSaga>();

        await _harness.Bus.Publish(new OrderCreateEventSuccess(orderId, UserId, cartItems, address));

        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEvent>(), Is.True);

        Guid? existsId = await sagaHarness.Exists(orderId, x => x.BookingGoodsInWarehouse);
        Assert.That(existsId.HasValue, Is.True, "Saga was not change to BookingGoodsInWarehouse");

    }

    [Test]
    public async Task Should_change_state_to_DeliverySend_when_goods_booked_success()
    {
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";
        var orderId = NewId.NextGuid();

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSaga>();

        await _harness.Bus.Publish(new OrderCreateEventSuccess(orderId, UserId, cartItems, address));
        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEventSuccess(orderId, UserId, cartItems, address));

        Assert.That(await _harness.Published.Any<DeliverySendEvent>(), Is.True);

        Guid? existsId = await sagaHarness.Exists(orderId, x => x.DeliverySend);
        Assert.That(existsId.HasValue, Is.True, "Saga was not change to DeliverySend");

    }

    [Test]
    public async Task Should_change_state_to_Rejected_when_goods_is_not_enough()
    {
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";
        var orderId = NewId.NextGuid();

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSaga>();

        await _harness.Bus.Publish(new OrderCreateEventSuccess(orderId, UserId, cartItems, address));

        await _harness.Bus.Publish(new InventoryGoodsBookedRejectedEvent(orderId, cartItems.ToDictionary(m => m.Id, m => m.Count)));

        Guid? existsId = await sagaHarness.Exists(orderId, x => x.Rejected);
        Assert.That(existsId.HasValue, Is.True, "Saga was not change to Rejected");
    }

    [Test]
    public async Task Should_change_state_to_Canceled_when_delivery_send_failed()
    {
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";
        var orderId = NewId.NextGuid();

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSaga>();

        await _harness.Bus.Publish(new OrderCreateEventSuccess(orderId, UserId, cartItems, address));

        Assert.That(await _harness.Published.Any<OrderCreateEventSuccess>(), Is.True);
        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEvent(orderId, UserId, cartItems,address));

        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEventSuccess(orderId, UserId, cartItems, address));

        await _harness.Bus.Publish(new DeliverySendEventFailed(orderId, cartItems));
        Assert.That(await _harness.Published.Any<InventoryGoodsRestoredEvent>(), Is.True);
        Assert.That(await _harness.Published.Any<OrderCancelEvent>(), Is.True);

        Guid? existsId = await sagaHarness.Exists(orderId, x => x.Canceled);
        Assert.That(existsId.HasValue, Is.True, "Saga was not change to Canceled");
    }

    [Test]
    public async Task Should_change_state_to_Failed_when_error_occurred_in_cancel_order()
    {
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";
        var orderId = NewId.NextGuid();

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSaga>();

        await _harness.Bus.Publish(new OrderCreateEventSuccess(orderId, UserId, cartItems, address));
        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEventFailed(orderId, cartItems));

        Assert.That((await _sagaHarness.Exists(orderId, x => x.Canceled)).HasValue, Is.True, "Saga must be in Canceled state first");

        await _harness.Bus.Publish<Fault<OrderCancelEvent>>(new
        {

            Message = new OrderCancelEvent(orderId),
            
            Timestamp = DateTime.UtcNow,

            Exceptions = new[] 
            { 
                new 
                { 
                    ExceptionType = "System.TimeoutException", 
                    Message = "Simulated DB Timeout during Order Cancellation" 
                } 
            }
        });

        Assert.That(await _sagaHarness.Consumed.Any<Fault<OrderCancelEvent>>(), Is.True, "Saga did not consume the Fault event");

        Guid? existsId = await sagaHarness.Exists(orderId, x => x.Failed);
        Assert.That(existsId.HasValue, Is.True, "Saga was not change to Failed");
    }

    public async Task Should_change_stage_to_Failed_when_error_occurred_in_goods_restored()
    {
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";
        var orderId = NewId.NextGuid();

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSaga>();

        await _harness.Bus.Publish(new OrderCreateEventSuccess(orderId, UserId, cartItems, address));
        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEvent(orderId, UserId, cartItems,address));

        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEvent(orderId, UserId, cartItems,address));
        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEventSuccess(orderId, UserId, cartItems, address));
        await _harness.Bus.Publish(new DeliverySendEventFailed(orderId, cartItems));

        Assert.That((await _sagaHarness.Exists(orderId, x => x.Canceled)).HasValue, Is.True, "Saga must be in Canceled state first");

        Assert.That(await _harness.Published.Any<OrderCancelEvent>(), Is.True);

        await _harness.Bus.Publish<Fault<InventoryGoodsRestoredEvent>>(new
        {

            Message = new InventoryGoodsRestoredEvent(orderId, cartItems.ToDictionary(m => m.Id, m => m.Count)),
            
            Timestamp = DateTime.UtcNow,

            Exceptions = new[] 
            { 
                new 
                { 
                    ExceptionType = "System.TimeoutException", 
                    Message = "Simulated DB Timeout during goods return" 
                } 
            }
        });

        Assert.That(await _sagaHarness.Consumed.Any<Fault<OrderCancelEvent>>(), Is.True, "Saga did not consume the Fault event");

        Guid? existsId = await sagaHarness.Exists(orderId, x => x.Failed);
        Assert.That(existsId.HasValue, Is.True, "Saga was not change to Failed");
    }

    #endregion
    
    #region 1. Happy Path
    
    [Test]
    public async Task HappyPath_Should_Reach_FinalState_When_All_Steps_Success()
    {

        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";
        var orderId = NewId.NextGuid();

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));
        
        await _harness.Bus.Publish(new OrderCreateEventSuccess(orderId, UserId, cartItems, address));
        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEvent>(), Is.True);
        Assert.That((await _sagaHarness.Exists(orderId, x => x.BookingGoodsInWarehouse)).HasValue, Is.True);

        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEventSuccess(orderId, UserId, cartItems,address));
        Assert.That(await _harness.Published.Any<DeliverySendEvent>(), Is.True);
        Assert.That((await _sagaHarness.Exists(orderId, x => x.DeliverySend)).HasValue, Is.True);

        await _harness.Bus.Publish(new DeliverySendEventSuccess(orderId));
        Assert.That((await _sagaHarness.Exists(orderId, x => x.Final)).HasValue, Is.True);
    }
    
    #endregion

    #region 2. Sad Paths

    [Test]
    public async Task SadPath_Should_Fail_And_CancelOrder_When_OrderCreate_Fails()
    {
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";
        var orderId = NewId.NextGuid();

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));

        await _harness.Bus.Publish(new OrderCreateEventFailed(orderId, cartItems));

        Assert.That(await _harness.Published.Any<OrderCancelEvent>(), Is.True);
        Assert.That((await _sagaHarness.Exists(orderId, x => x.Canceled)).HasValue, Is.True);
    }

    [Test]
    public async Task SadPath_Should_Fail_And_CancelOrder_When_BookingGoods_Fails()
    {

        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";
        var orderId = NewId.NextGuid();

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));
        await _harness.Bus.Publish(new OrderCreateEventSuccess(orderId, UserId, cartItems, address));

        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEventFailed(orderId, cartItems));

        Assert.That(await _harness.Published.Any<OrderCancelEvent>(), Is.True);
        Assert.That((await _sagaHarness.Exists(orderId, x => x.Canceled)).HasValue, Is.True);
    }

    [Test]
    public async Task SadPath_Should_RestoreInventory_And_CancelOrder_When_Delivery_Fails()
    {
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";
        var orderId = NewId.NextGuid();

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));
        await _harness.Bus.Publish(new OrderCreateEventSuccess(orderId, UserId, cartItems, address));

        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEventSuccess(orderId, UserId, cartItems, address));

        await _harness.Bus.Publish(new DeliverySendEventFailed(orderId, cartItems));

        Assert.That(await _harness.Published.Any<InventoryGoodsRestoredEvent>(), Is.True, "Must return goods to warehouse");
        Assert.That(await _harness.Published.Any<OrderCancelEvent>(), Is.True, "Must cancel the order");

        Assert.That((await _sagaHarness.Exists(orderId, x => x.Canceled)).HasValue, Is.True);
    }

    #endregion
}
