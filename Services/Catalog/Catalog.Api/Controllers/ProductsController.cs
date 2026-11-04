using Catalog.Api.Metrics;
using Catalog.Bll.Dtos;
using Catalog.Bll.Queries;
using Catalog.Bll.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    private readonly CatalogMetrics _metrics;

    public ProductsController(IProductService products, CatalogMetrics metrics)
    {
        _products = products;
        _metrics = metrics;
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<ProductListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> GetList(
        [FromQuery] ProductQuery query, [FromServices] IValidator<ProductQuery> validator, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

        var result = await _products.GetListAsync(query, ct);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
        return Ok(result);
    }

    [HttpGet("low-stock")]
    [ProducesResponseType<IReadOnlyList<ProductListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProductListItemDto>>> GetLowStock(
        [FromQuery] int threshold = 5, CancellationToken ct = default)
        => Ok(await _products.GetLowStockAsync(threshold, ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(long id, CancellationToken ct)
        => Ok(await _metrics.MeasureAsync("get", () => _products.GetByIdAsync(id, ct)));

    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create(
        CreateProductDto input, [FromServices] IValidator<CreateProductDto> validator, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

        var created = await _metrics.MeasureAsync("create", () => _products.CreateAsync(input, ct));
        _metrics.ProductCreated(created.IsActive);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}/price")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> ChangePrice(
        long id, ChangePriceDto input, [FromServices] IValidator<ChangePriceDto> validator, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

        var before = await _products.GetByIdAsync(id, ct);
        var updated = await _metrics.MeasureAsync("change_price", () => _products.ChangePriceAsync(id, input, ct));
        _metrics.PriceChanged(before.Price, updated.Price);
        return Ok(updated);
    }

    [HttpPost("{id:long}/stock/add")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> AddStock(long id, StockChangeDto input, CancellationToken ct)
    {
        var product = await _metrics.MeasureAsync("add_stock", () => _products.AddStockAsync(id, input, ct));
        _metrics.StockChanged("added");
        return Ok(product);
    }

    [HttpPost("{id:long}/stock/remove")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> RemoveStock(long id, StockChangeDto input, CancellationToken ct)
    {
        var product = await _metrics.MeasureAsync("remove_stock", () => _products.RemoveStockAsync(id, input, ct));
        _metrics.StockChanged("removed");
        return Ok(product);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _metrics.MeasureAsync("delete", () => _products.DeleteAsync(id, ct));
        _metrics.ProductDeleted();
        return NoContent();
    }
}
