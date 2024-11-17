using Evacuation.DTO.EvacuationUpdate;
using Evacuation.DTO.EvacuationZones;
using Evacuation.Mediator.AddEvacuationZone;
using Evacuation.Mediator.EvacuationPlan;
using Evacuation.Mediator.EvacuationUpateStatus;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EvacuationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EvacuationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("plan")]
    public async Task<IActionResult> PlanEvacuation()
    {
        var result = await _mediator.Send(new EvacuationPlanQuery());
        return Ok(result);
    }

    [HttpGet("status")]
    public async Task<IActionResult> StatusEvacuation()
    {
        var result = await _mediator.Send(new GetEvacuationStatusQuery());
        return Ok(result);
    }


    [HttpPost("update")]
    public async Task<IActionResult> UpdateEvacuation(List<EvacuationUpdateDTO> evacuationUpdates)
    {
        var result = await _mediator.Send(new UpdateEvacuationStatusCmd(evacuationUpdates));
        return Ok(result);
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearEvacuation()
    {
        var result = await _mediator.Send(new RemoveEvacuationPlanCmd());
        return Ok(result);
    }


}
