using BacklogApi.Application.DTOs;
using BacklogApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BacklogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BacklogController : ControllerBase
{
    private readonly IBacklogService _service;
    private readonly ICommentService _commentService;

    public BacklogController(IBacklogService service, ICommentService commentService)
    {
        _service = service;
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BacklogItemDto>>> GetAll([FromQuery] int? boardId)
    {
        if (!boardId.HasValue)
        {
            if (User.IsInRole("Admin"))
            {
                return Ok(await _service.GetAllAsync(null));
            }
            return Ok(Enumerable.Empty<BacklogItemDto>());
        }
        return Ok(await _service.GetAllAsync(boardId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BacklogItemDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<BacklogItemDto>> Create(CreateBacklogItemDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var author = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("unique_name") ?? "Unknown";
        var item = await _service.CreateAsync(dto, author);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateBacklogItemDto dto)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();

        var currentUser = User.Identity?.Name;
        var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

        if (item.CreatedBy != currentUser && !isAdminOrManager)
        {
            return Forbid();
        }

        var success = await _service.UpdateAsync(id, dto, currentUser ?? "Unknown");
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();

        var currentUser = User.Identity?.Name ?? "Unknown";
        var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

        if (item.CreatedBy != currentUser && !isAdminOrManager)
        {
            return Forbid();
        }

        var success = await _service.DeleteAsync(id, currentUser);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder(IEnumerable<ReorderBacklogItemDto> dtos)
    {
        var currentUser = User.Identity?.Name ?? "Unknown";
        await _service.ReorderAsync(dtos, currentUser);
        return NoContent();
    }

    [HttpGet("{id}/comments")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(int id)
    {
        return Ok(await _commentService.GetByBacklogItemIdAsync(id));
    }

    [HttpPost("{id}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(int id, CreateCommentDto dto)
    {
        var author = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("unique_name") ?? "Unknown";
        var comment = await _commentService.AddCommentAsync(id, dto.Content, author);
        return Ok(comment);
    }

    [HttpPut("comments/{commentId}")]
    public async Task<IActionResult> UpdateComment(int commentId, CreateCommentDto dto)
    {
        var author = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("unique_name") ?? "Unknown";
        var success = await _commentService.UpdateCommentAsync(commentId, dto.Content, author);
        if (!success) return Forbid();
        return NoContent();
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var author = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("unique_name") ?? "Unknown";
        var success = await _commentService.DeleteCommentAsync(commentId, author);
        if (!success) return Forbid();
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _service;

    public AttachmentsController(IAttachmentService service)
    {
        _service = service;
    }

    [HttpPost("{backlogItemId}")]
    public async Task<ActionResult<AttachmentDto>> Upload(int backlogItemId, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded");

        using (var stream = file.OpenReadStream())
        {
            var result = await _service.UploadAsync(backlogItemId, file.FileName, file.ContentType, stream);
            return Ok(result);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Download(int id)
    {
        var attachment = await _service.GetByIdAsync(id);
        if (attachment == null) return NotFound();

        var bytes = await System.IO.File.ReadAllBytesAsync(attachment.FilePath);
        return File(bytes, attachment.ContentType, attachment.FileName);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ColumnsController : ControllerBase
{
    private readonly IColumnService _service;

    public ColumnsController(IColumnService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BoardColumnDto>>> GetAll([FromQuery] int? boardId)
    {
        return Ok(await _service.GetAllAsync(boardId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BoardColumnDto>> GetById(int id)
    {
        var column = await _service.GetByIdAsync(id);
        if (column == null) return NotFound();
        return Ok(column);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<BoardColumnDto>> Create(CreateBoardColumnDto dto)
    {
        var column = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = column.Id }, column);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, CreateBoardColumnDto dto)
    {
        var success = await _service.UpdateAsync(id, dto);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SprintsController : ControllerBase
{
    private readonly ISprintService _service;

    public SprintsController(ISprintService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SprintDto>>> GetAll([FromQuery] int? boardId)
    {
        return Ok(await _service.GetAllAsync(boardId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SprintDto>> GetById(int id)
    {
        var sprint = await _service.GetByIdAsync(id);
        if (sprint == null) return NotFound();
        return Ok(sprint);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SprintDto>> Create(CreateSprintDto dto)
    {
        var sprint = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = sprint.Id }, sprint);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, CreateSprintDto dto)
    {
        var success = await _service.UpdateAsync(id, dto);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}
