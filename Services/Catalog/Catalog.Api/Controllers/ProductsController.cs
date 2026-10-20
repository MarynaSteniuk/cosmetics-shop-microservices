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

    public ProductsController(IProductService products)
    {
        _products = products;
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
        => Ok(await _products.GetByIdAsync(id, ct));

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

        var created = await _products.CreateAsync(input, ct);
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

        return Ok(await _products.ChangePriceAsync(id, input, ct));
    }

    [HttpPost("{id:long}/stock/add")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> AddStock(long id, StockChangeDto input, CancellationToken ct)
        => Ok(await _products.AddStockAsync(id, input, ct));

    [HttpPost("{id:long}/stock/remove")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> RemoveStock(long id, StockChangeDto input, CancellationToken ct)
        => Ok(await _products.RemoveStockAsync(id, input, ct));

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _products.DeleteAsync(id, ct);
        return NoContent();
    }
}
