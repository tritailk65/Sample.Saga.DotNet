
global using Choreography.Inventory.IntegrationeEvent.EventHandling;
global using Choreography.Order.IntegrationEvent.EventHandling;
global using MassTransit;
global using MassTransit.Testing;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Shared.Contracts;
global using System.Reflection;
global using Microsoft.EntityFrameworkCore.Design;

global using Choreography.Delivery.IntegrationEvent.EventHandling;


global using Shared.Services.Delivery.Services;
global using Shared.Services.Inventory.Services;
global using Shared.Services.Order;

global using Shared.Infrastructure.Delivery.Infrastructure;
global using Shared.Infrastructure.Inventory.Infrastructure;
global using Shared.Infrastructure.Inventory.Infrastructure.Entities;
global using Shared.Infrastructure.Order.Infrastructure;
global using Shared.Infrastructure.Order.Infrastructure.Enums;

global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;