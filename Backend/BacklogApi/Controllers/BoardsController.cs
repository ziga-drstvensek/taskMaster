using BacklogApi.Application.DTOs;
using BacklogApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BacklogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BoardsController : ControllerBase
{
    private readonly IBoardService _service;

    public BoardsController(IBoardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BoardDto>>> GetAll()
    {
        if (User.IsInRole("Admin"))
        {
            return Ok(await _service.GetAllAsync());
        }
        return Ok(await _service.GetByUsernameAsync(User.Identity!.Name!));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BoardDto>> GetById(int id)
    {
        var board = await _service.GetByIdAsync(id);
        if (board == null) return NotFound();
        
        if (!User.IsInRole("Admin") && !board.Usernames.Contains(User.Identity!.Name!))
        {
            return Forbid();
        }
        
        return Ok(board);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<BoardDto>> Create(CreateBoardDto dto)
    {
        var board = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = board.Id }, board);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, CreateBoardDto dto)
    {
        var success = await _service.UpdateAsync(id, dto);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}
