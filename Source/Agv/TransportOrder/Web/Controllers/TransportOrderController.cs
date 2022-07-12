using Microsoft.AspNetCore.Mvc;
using Gebhardt.StoreWare.Agv.Common.Messages;
using MassTransit.Saga;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers
{
    /// <summary>
    /// Controller for host order data
    /// </summary>
    [ApiController]
    public class TransportOrderController : Controller
    {
        private readonly SagaRepository<OrderSaga> _repo;

        /// <summary>
        /// Init controller
        /// </summary>
        public TransportOrderController(ISagaRepository<OrderSaga> repo)
        {
            _repo = (SagaRepository<OrderSaga>)repo;
        }

        /// <summary>
        /// Retrieve active transport orders
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = new List<Order>();

            // Get corallation ids of all order sagas
            IEnumerable<Guid> corrationIds = await _repo.Find(new SagaQuery<OrderSaga>(_ => true)); // Query that returns all order sagas
            foreach (var corallationId in corrationIds)
            {
                var orderSaga = await _repo.Load(corallationId); // Load order saga with corallation id
                if (orderSaga != null) // Null check in case repo has changed between query and load
                {
                    orders.Add(orderSaga.Order);
                }
            }

            return Ok(orders);
        }
    }
}
