using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories)
    {
        _categories = categories;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken ct)
        => Ok(await _categories.GetAllAsync(ct));

    [HttpGet("{id:int}/products")]
    [ProducesResponseType<CategoryWithProductsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryWithProductsDto>> GetWithProducts(int id, CancellationToken ct)
        => Ok(await _categories.GetWithProductsAsync(id, ct));

    [HttpGet("{slug}/recent")]
    [ProducesResponseType<IReadOnlyList<RecentlyAddedProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<RecentlyAddedProductDto>>> GetRecentlyAdded(
        string slug, [FromQuery] DateTime? since, CancellationToken ct)
        => Ok(await _categories.GetRecentlyAddedAsync(slug, since, ct));
}
