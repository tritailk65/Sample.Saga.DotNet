
namespace Shared.StateMachines;
using MassTransit;
using Shared.Contracts;

public class OrderSaga : SagaStateMachineInstance
{
    // Payload Data
    public IEnumerable<GoodViewModel> Goods { get; set; } = null!;
    public string DeliveryAddress { get; set; } = null!;
    public Guid UserId { get; set; }
    
    // State
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;
    public int Version { get; set; }
    
    // Audit info
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdateAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Callback 
    public Guid? RequestId { get; set; }
    public Uri? ResponseAddress { get; set; }
}

