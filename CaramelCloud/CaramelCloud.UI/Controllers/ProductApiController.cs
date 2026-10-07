using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.UI.Models.Api;
using Microsoft.AspNetCore.Mvc;

namespace CaramelCloud.UI.Controllers
{
    [ApiController]
    [Route("api/products")]
    [Produces("application/json")]
    public class ProductApiController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public ProductApiController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
        public IActionResult GetAll([FromQuery] bool? isAvailable = null)
        {
            var products = _uow.Product.GetAll(
                filter: isAvailable.HasValue
                    ? p => p.IsAvailable == isAvailable.Value
                    : null,
                includeProperties: "Category");

            var result = products.Select(MapToDto);
            return Ok(result);
        }

        
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetById(int id)
        {
            var product = _uow.Product.Get(
                p => p.Id == id,
                includeProperties: "Category");

            if (product is null)
                return NotFound(new { message = $"Продукт з ID {id} не знайдено" });

            return Ok(MapToDto(product));
        }

        
        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Create([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                ImageUrl = dto.ImageUrl,
                CategoryId = dto.CategoryId,
                RecipeId = dto.RecipeId,
                Weight = dto.Weight,
                ServingSize = dto.ServingSize,
                IsAvailable = dto.IsAvailable,
                CreatedAt = DateTime.UtcNow,
            };

            _uow.Product.Add(product);
            _uow.Save();

            var created = _uow.Product.Get(
                p => p.Id == product.Id,
                includeProperties: "Category");

            return CreatedAtAction(
                nameof(GetById),
                new { id = product.Id },
                MapToDto(created!));
        }

        
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Update(int id, [FromBody] UpdateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = _uow.Product.Get(p => p.Id == id, tracked: true);
            if (existing is null)
                return NotFound(new { message = $"Продукт з ID {id} не знайдено" });

            existing.Name = dto.Name.Trim();
            existing.Description = dto.Description?.Trim();
            existing.Price = dto.Price;
            existing.ImageUrl = dto.ImageUrl;
            existing.CategoryId = dto.CategoryId;
            existing.RecipeId = dto.RecipeId;
            existing.Weight = dto.Weight;
            existing.ServingSize = dto.ServingSize;
            existing.IsAvailable = dto.IsAvailable;

            _uow.Product.Update(existing);
            _uow.Save();

            var updated = _uow.Product.Get(
                p => p.Id == id,
                includeProperties: "Category");

            return Ok(MapToDto(updated!));
        }

        
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public IActionResult Delete(int id)
        {
            var product = _uow.Product.Get(p => p.Id == id);
            if (product is null)
                return NotFound(new { message = $"Продукт з ID {id} не знайдено" });

            var isInOrders = _uow.OrderItem
                .GetAll(oi => oi.ProductId == id)
                .Any();

            if (isInOrders)
                return Conflict(new { message = "Неможливо видалити — продукт використовується в замовленнях" });

            _uow.Product.Remove(product);
            _uow.Save();

            return NoContent();
        }

        private static ProductDto MapToDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            ImageUrl = p.ImageUrl,
            CategoryName = p.Category?.Name ?? "—",
            Weight = p.Weight,
            ServingSize = p.ServingSize,
            IsAvailable = p.IsAvailable,
            CreatedAt = p.CreatedAt,
        };
    }
}
