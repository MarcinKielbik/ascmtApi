using JustInTimeApi.Context;
using JustInTimeApi.Dto;
using JustInTimeApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JustInTimeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class SupplierController : ControllerBase
    {
        private readonly AppDbContext _suppliers;

        public SupplierController(AppDbContext suppliers)
        {
            _suppliers = suppliers;
        }

        private int GetAdminId()
        {
            //return 1;
            
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId))
            {
                throw new UnauthorizedAccessException("Admin ID cannot be null or empty. Ensure the token is valid and includes the 'NameIdentifier' claim.");
            }
            return int.Parse(adminId);
        }

        [HttpGet]
        public async Task<ActionResult<List<Supplier>>> GetSuppliers()
        {
            try
            {
                var adminId = GetAdminId();
                return await _suppliers.Suppliers.Where(s => s.UserId == adminId).ToListAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Supplier>> GetSupplier(int id)
        {
            try
            {
                var adminId = GetAdminId();
                var supplier = await _suppliers.Suppliers
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == adminId);

                if (supplier == null)
                {
                    return NotFound();
                }

                return supplier;
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<Supplier>> AddSupplier(SupplierDto supplierDto)
        {
            try
            {
                if (supplierDto == null)
                    return BadRequest("Nieprawidłowe dane dostawcy.");

                // Sprawdź, czy dostawca o tym samym adresie e-mail już istnieje dla dowolnego administratora
                if (await _suppliers.Suppliers.AnyAsync(s => s.Email == supplierDto.Email))
                {
                    return BadRequest(new { Message = "Dostawca o tym adresie e-mail już istnieje." });
                }

                if (supplierDto.Password.Length < 8)
                {
                    return BadRequest(new { Message = "Hasło musi zawierać co najmniej 8 znaków." });
                }

                var adminId = GetAdminId();

                var supplier = new Supplier
                {
                    FirstName = supplierDto.FirstName,
                    LastName = supplierDto.LastName,
                    Email = supplierDto.Email,
                    PhoneNumber = supplierDto.PhoneNumber,
                    Password = PasswordHasher.HashPassword(supplierDto.Password),
                    CompanyName = supplierDto.CompanyName,
                    Role = "Supplier",
                    RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
                    UserId = adminId
                };

                _suppliers.Suppliers.Add(supplier);
                await _suppliers.SaveChangesAsync();

                return Ok(new { Message = "Dostawca został pomyślnie dodany." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, SupplierDto supplierDto)
        {
            try
            {
                var adminId = GetAdminId();
                var supplier = await _suppliers.Suppliers
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == adminId);

                if (supplier == null)
                {
                    return BadRequest("Nie znaleziono dostawcy.");
                }

                // Sprawdź, czy istnieje już dostawca z takim adresem e-mail, inny niż ten aktualizowany
                if (await _suppliers.Suppliers.AnyAsync(s => s.Email == supplierDto.Email && s.Id != id))
                {
                    return BadRequest(new { Message = "Dostawca o tym adresie e-mail już istnieje." });
                }

                supplier.Email = supplierDto.Email;
                supplier.FirstName = supplierDto.FirstName;
                supplier.LastName = supplierDto.LastName;
                supplier.Password = PasswordHasher.HashPassword(supplierDto.Password);
                supplier.CompanyName = supplierDto.CompanyName;
                supplier.PhoneNumber = supplierDto.PhoneNumber;

                _suppliers.Entry(supplier).State = EntityState.Modified;
                await _suppliers.SaveChangesAsync();

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                var adminId = GetAdminId();
                var supplier = await _suppliers.Suppliers
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == adminId);

                if (supplier == null)
                {
                    return BadRequest("Supplier not found");
                }

                _suppliers.Suppliers.Remove(supplier);
                await _suppliers.SaveChangesAsync();

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        /*
        [HttpPost("{id}/orders")]
        public async Task<IActionResult> SendOrder(int id, OrderDto orderDto)
        {
            try
            {
                var adminId = GetAdminId();
                var supplier = await _suppliers.Suppliers
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == adminId);

                if (supplier == null)
                {
                    return NotFound();
                }

                var order = new Order
                {
                    SupplierId = id,
                    ProductName = orderDto.ProductName,
                    Quantity = orderDto.Quantity,
                    OrderDate = orderDto.OrderDate,
                    DueDate = orderDto.DueDate,
                };

                _suppliers.Orders.Add(order);
                await _suppliers.SaveChangesAsync();

                return CreatedAtAction("GetOrder", new { id = order.Id }, order);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }*/
    }
}
