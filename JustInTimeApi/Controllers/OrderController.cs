using JustInTimeApi.Context;
using JustInTimeApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using JustInTimeApi.Dto;
using JustInTimeApi.Extensions;

namespace JustInTimeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin, Supplier")]
        public async Task<ActionResult<List<Order>>> GetOrders()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var userRole = User.FindFirstValue(ClaimTypes.Role);

                if (userRole == "Admin")
                {
                    var orders = await _context.Orders
                        .Include(o => o.Supplier)
                        .Include(o => o.User)
                        .Where(o => o.UserId == userId)
                        .ToListAsync();

                    return Ok(orders);
                }
                else if (userRole == "Supplier")
                {
                    var supplier = await _context.Suppliers
                        .Include(s => s.Orders)
                        .ThenInclude(o => o.User)
                        .FirstOrDefaultAsync(s => s.Id == userId);

                    if (supplier == null)
                    {
                        return NotFound("Supplier not found.");
                    }

                    var orders = supplier.Orders.ToList();

                    return Ok(orders);
                }
                else
                {
                    return Forbid("Unauthorized access.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error: " + ex.Message);
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var userRole = User.FindFirstValue(ClaimTypes.Role);

                var order = await _context.Orders
                    .Include(o => o.Supplier)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound("Order not found.");
                }

                if (userRole == "Admin" && order.UserId == userId)
                {
                    return Ok(order);
                }
                else if (userRole == "Supplier" && order.SupplierId == userId)
                {
                    return Ok(order);
                }
                else
                {
                    return Forbid("Unauthorized access.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error: " + ex.Message);
            }
        }

        /*
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Order>> CreateOrder(Order order)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                order.UserId = userId;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error: " + ex.Message);
            }
        }*/


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<Order>>> CreateOrder([FromBody] OrderDto orderDto)
        {
            if (orderDto == null)
                return BadRequest("Invalid order data.");

            var supplier = await _context.Suppliers.FindAsync(orderDto.SupplierId);
            if (supplier == null)
                return BadRequest("Invalid supplier ID.");


            var userId = Int32.Parse(User.GetUserId());

            var order = new Order
            {
                ProductName = orderDto.ProductName,
                Quantity = orderDto.Quantity,
                PricePerUnit = orderDto.PricePerUnit,
                Currency = orderDto.Currency,
                PickupLocation = orderDto.PickupLocation,
                Destination = orderDto.Destination,
                OrderDate = orderDto.OrderDate,
                DueDate = orderDto.DueDate,
                Status = orderDto.Status,
                SupplierId = orderDto.SupplierId,
                UserId = userId
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order created successfully.", OrderId = order.Id });
        }
        /*
        [HttpPut("{id}")]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string status)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var existingOrder = await _context.Orders.FindAsync(id);

                if (existingOrder == null)
                {
                    return NotFound("Order not found.");
                }

                if (existingOrder.UserId != userId && existingOrder.SupplierId != userId)
                {
                    return Forbid("Unauthorized access.");
                }

                existingOrder.Status = status;
                _context.Entry(existingOrder).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(existingOrder);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error: " + ex.Message);
            }
        }*/

        [HttpPut("{id}")]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto updateOrderStatusDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var existingOrder = await _context.Orders.FindAsync(id);

                if (existingOrder == null)
                {
                    return NotFound("Order not found.");
                }

                if (existingOrder.UserId != userId && existingOrder.SupplierId != userId)
                {
                    return Forbid("Unauthorized access.");
                }

                existingOrder.Status = updateOrderStatusDto.Status;
                _context.Entry(existingOrder).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(existingOrder);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error: " + ex.Message);
            }
        }

    }
}