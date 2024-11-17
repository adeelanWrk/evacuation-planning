using Evacuation.DTO.EvacuationZones;
using Evacuation.Mediator.AddEvacuationZone;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EvacuationZonesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EvacuationZonesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> AddEvacuationZone([FromBody] List<EvacuationZonesDTO> EvacuationZones)
    {
        var result = await _mediator.Send(new AddEvacuationZoneCmd(EvacuationZones));
        return Ok(result);
    }

  
}
