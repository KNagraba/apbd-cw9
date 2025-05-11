using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/warehouses")]
public class WarehouseController : ControllerBase
{
    private readonly IDbService _dbService;

    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost]
    public async Task<IActionResult> AddProductToWarehouse([FromBody] WarehouseRequest request)
    {
        try
        {
            int id = await _dbService.AddProductToWarehouseAsync(request);
            return Ok(new { IdProductWarehouse = id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "Unexpected server error.");
        }
    }
    
    [HttpPost("procedure")]
    public async Task<IActionResult> AddProductToWarehouseUsingProcedure([FromBody] WarehouseRequest request)
    {
        try
        {
            int id = await _dbService.AddProductToWarehouseViaProcedureAsync(request);
            return Ok(new { IdProductWarehouse = id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Unexpected server error: {ex.Message}");
        }
    }

}