
namespace Shared.Infrastructure.OrderSaga;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.StateMachines;

public class OrderClassMap : SagaClassMap<OrderSaga>
{
    protected override void Configure(EntityTypeBuilder<OrderSaga> entity, ModelBuilder model)
    {

    }
}