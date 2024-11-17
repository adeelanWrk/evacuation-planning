using Evacuation.DTO.Vehicle;
using Evacuation.Mediator.Vehicle;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class VehicleController : ControllerBase
{
    private readonly IMediator _mediator;

    public VehicleController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpPost]
    public async Task<IActionResult> AddVehicle([FromBody] List<VehicleDTO> Vehicles)
    {
        var result = await _mediator.Send(new AddVehicleCmd(Vehicles));
        return Ok(result);
    }
}
