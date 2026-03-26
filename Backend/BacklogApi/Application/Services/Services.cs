using BacklogApi.Application.DTOs;
using BacklogApi.Core.Entities;
using BacklogApi.Core.Interfaces;
using BacklogApi.Infrastructure.Hubs;
using BacklogApi.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace BacklogApi.Application.Services;

public interface IBacklogService
{
    Task<IEnumerable<BacklogItemDto>> GetAllAsync(int? boardId = null);
    Task<BacklogItemDto?> GetByIdAsync(int id);
    Task<BacklogItemDto> CreateAsync(CreateBacklogItemDto dto, string createdBy);
    Task<bool> UpdateAsync(int id, UpdateBacklogItemDto dto, string updatedBy);
    Task<bool> DeleteAsync(int id);
    Task ReorderAsync(IEnumerable<ReorderBacklogItemDto> dto, string updatedBy);
}

public interface IAttachmentService
{
    Task<AttachmentDto> UploadAsync(int backlogItemId, string fileName, string contentType, Stream content);
    Task<Attachment?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
}

public interface IColumnService
{
    Task<IEnumerable<BoardColumnDto>> GetAllAsync(int? boardId = null);
    Task<BoardColumnDto?> GetByIdAsync(int id);
    Task<BoardColumnDto> CreateAsync(CreateBoardColumnDto dto);
    Task<bool> UpdateAsync(int id, CreateBoardColumnDto dto);
    Task<bool> DeleteAsync(int id);
}

public interface ISprintService
{
    Task<IEnumerable<SprintDto>> GetAllAsync(int? boardId = null);
    Task<SprintDto?> GetByIdAsync(int id);
    Task<SprintDto> CreateAsync(CreateSprintDto dto);
    Task<bool> UpdateAsync(int id, CreateSprintDto dto);
    Task<bool> DeleteAsync(int id);
}

public interface ICommentService
{
    Task<IEnumerable<CommentDto>> GetByBacklogItemIdAsync(int backlogItemId);
    Task<CommentDto> AddCommentAsync(int backlogItemId, string content, string author);
    Task<bool> UpdateCommentAsync(int commentId, string content, string author);
    Task<bool> DeleteCommentAsync(int commentId, string author);
}

public interface IBoardService
{
    Task<IEnumerable<BoardDto>> GetAllAsync();
    Task<IEnumerable<BoardDto>> GetByUsernameAsync(string username);
    Task<BoardDto?> GetByIdAsync(int id);
    Task<BoardDto> CreateAsync(CreateBoardDto dto);
    Task<bool> UpdateAsync(int id, CreateBoardDto dto);
    Task<bool> DeleteAsync(int id);
}

public interface ISmtpSettingsService
{
    Task<SmtpSettingsDto?> GetSettingsAsync();
    Task<bool> UpdateSettingsAsync(UpdateSmtpSettingsDto dto);
    Task<bool> SendEmailAsync(string to, string subject, string body);
}

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string username);
    Task<bool> MarkAsReadAsync(int notificationId, string username);
    Task<bool> MarkAllAsReadAsync(string username);
    Task CreateNotificationAsync(string username, string title, string message, string? link = null, string? type = null);
}

public class BacklogService : IBacklogService
{
    private readonly IBacklogRepository _repository;
    private readonly IColumnRepository _columnRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly IHubContext<BacklogHub> _hubContext;
    private readonly INotificationService _notificationService;

    public BacklogService(
        IBacklogRepository repository, 
        IColumnRepository columnRepository,
        ISprintRepository sprintRepository,
        IHubContext<BacklogHub> hubContext,
        INotificationService notificationService)
    {
        _repository = repository;
        _columnRepository = columnRepository;
        _sprintRepository = sprintRepository;
        _hubContext = hubContext;
        _notificationService = notificationService;
    }

    public async Task<IEnumerable<BacklogItemDto>> GetAllAsync(int? boardId = null)
    {
        var items = await _repository.GetAllAsync(boardId);
        return items.Select(MapToDto);
    }

    public async Task<BacklogItemDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    public async Task<BacklogItemDto> CreateAsync(CreateBacklogItemDto dto, string createdBy)
    {
        var boardId = dto.BoardId == 0 ? null : dto.BoardId;
        var columnId = dto.ColumnId;
        
        // Safety check: validate column belongs to board
        var column = await _columnRepository.GetByIdAsync(columnId);
        if (column == null || (boardId.HasValue && column.BoardId != boardId))
        {
            // Če je stolpec neveljaven za to ploščo, poskusimo dobiti prvi stolpec te plošče
            if (boardId.HasValue)
            {
                var boardColumns = await _columnRepository.GetAllAsync(boardId.Value);
                var firstColumn = boardColumns.OrderBy(c => c.Order).FirstOrDefault();
                if (firstColumn != null)
                {
                    columnId = firstColumn.Id;
                }
                else
                {
                    throw new Exception("Izbrana plošča nima definiranih stolpcev.");
                }
            }
            else
            {
                // Če ni plošče, morda pustimo trenutni stolpec ali pa vržemo napako?
                // Vrnemo napako, ker brez plošče ne vemo kateri stolpec je pravilen (razen če so globalni)
                if (column == null) throw new Exception("Neveljaven stolpec.");
            }
        }

        // Safety check: validate sprint belongs to board
        var sprintId = dto.SprintId;
        if (sprintId.HasValue)
        {
            var sprint = await _sprintRepository.GetByIdAsync(sprintId.Value);
            if (sprint == null || (boardId.HasValue && sprint.BoardId != boardId))
            {
                // Če sprint ne pripada plošči, ga enostavno odstranimo (nastavimo na null)
                sprintId = null;
            }
        }

        var item = new BacklogItem
        {
            Title = dto.Title,
            Description = dto.Description,
            ColumnId = columnId,
            BoardId = boardId,
            Priority = dto.Priority,
            SprintId = sprintId,
            ParentId = dto.ParentId,
            CreatedBy = createdBy,
            AssignedTo = dto.AssignedTo,
            DueDate = dto.DueDate,
            Order = await _repository.GetNextOrderAsync()
        };

        item.History.Add(new BacklogItemHistory
        {
            ChangeType = "Created",
            Description = $"Opravilo ustvarjeno s strani {createdBy}",
            ChangedBy = createdBy
        });

        await _repository.AddAsync(item);
        await _repository.SaveChangesAsync();

        if (!string.IsNullOrEmpty(item.AssignedTo) && item.AssignedTo != createdBy)
        {
            await _notificationService.CreateNotificationAsync(
                item.AssignedTo,
                "Dodeljeno opravilo",
                $"Dodeljeni ste bili k opravilu: {item.Title}",
                $"/task/{item.Id}",
                "TaskAssigned"
            );
        }

        await _hubContext.Clients.All.SendAsync("ItemsUpdated");

        return MapToDto(item);
    }

    public async Task<bool> UpdateAsync(int id, UpdateBacklogItemDto dto, string updatedBy)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null) return false;

        var newBoardId = dto.BoardId == 0 ? null : dto.BoardId;
        var newColumnId = dto.ColumnId;

        // Safety check: validate column belongs to board
        var column = await _columnRepository.GetByIdAsync(newColumnId);
        if (column == null || (newBoardId.HasValue && column.BoardId != newBoardId))
        {
            if (newBoardId.HasValue)
            {
                var boardColumns = await _columnRepository.GetAllAsync(newBoardId.Value);
                var firstColumn = boardColumns.OrderBy(c => c.Order).FirstOrDefault();
                if (firstColumn != null)
                {
                    newColumnId = firstColumn.Id;
                }
            }
        }

        // Safety check: validate sprint belongs to board
        var newSprintId = dto.SprintId;
        if (newSprintId.HasValue)
        {
            var sprint = await _sprintRepository.GetByIdAsync(newSprintId.Value);
            if (sprint == null || (newBoardId.HasValue && sprint.BoardId != newBoardId))
            {
                newSprintId = null;
            }
        }

        var changes = new List<string>();
        if (item.Title != dto.Title) changes.Add($"Naslov: '{item.Title}' -> '{dto.Title}'");
        if (item.Description != dto.Description) changes.Add("Opis je bil spremenjen");
        if (item.ColumnId != newColumnId) changes.Add($"Stolpec: {item.ColumnId} -> {newColumnId}");
        if (item.Priority != dto.Priority) changes.Add($"Prioriteta: {item.Priority} -> {dto.Priority}");
        if (item.BoardId != newBoardId) 
        {
            var oldBoardName = item.BoardId.HasValue ? $"Plošča ID {item.BoardId}" : "Brez plošče";
            var newBoardName = newBoardId.HasValue ? $"Plošča ID {newBoardId}" : "Brez plošče";
            changes.Add($"Plošča: {oldBoardName} -> {newBoardName}");
        }
        if (item.SprintId != newSprintId) changes.Add($"Sprint: {item.SprintId} -> {newSprintId}");
        if (item.AssignedTo != dto.AssignedTo) changes.Add($"Izvajalec: '{item.AssignedTo}' -> '{dto.AssignedTo}'");
        if (item.DueDate != dto.DueDate) changes.Add($"Rok: {item.DueDate} -> {dto.DueDate}");

        if (changes.Count > 0)
        {
            item.History.Add(new BacklogItemHistory
            {
                ChangeType = "Updated",
                Description = string.Join("; ", changes),
                ChangedBy = updatedBy
            });
        }

        var oldAssignee = item.AssignedTo;
        var newAssignee = dto.AssignedTo;

        item.Title = dto.Title;
        item.Description = dto.Description;
        item.ColumnId = newColumnId;
        item.BoardId = newBoardId;
        item.Priority = dto.Priority;
        item.SprintId = newSprintId;
        item.ParentId = dto.ParentId;
        item.AssignedTo = newAssignee;
        item.DueDate = dto.DueDate;

        _repository.Update(item);
        await _repository.SaveChangesAsync();

        if (oldAssignee != newAssignee && !string.IsNullOrEmpty(newAssignee) && newAssignee != updatedBy)
        {
            await _notificationService.CreateNotificationAsync(
                newAssignee,
                "Dodeljeno opravilo",
                $"Dodeljeni ste bili k opravilu: {item.Title}",
                $"/task/{item.Id}",
                "TaskAssigned"
            );
        }

        if (oldAssignee != newAssignee && !string.IsNullOrEmpty(oldAssignee) && oldAssignee != updatedBy)
        {
            await _notificationService.CreateNotificationAsync(
                oldAssignee,
                "Odstranjeni iz opravila",
                $"Niste več dodeljeni k opravilu: {item.Title}",
                $"/task/{item.Id}",
                "TaskRemoved"
            );
        }

        await _hubContext.Clients.All.SendAsync("ItemsUpdated");

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null) return false;

        _repository.Delete(item);
        await _repository.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("ItemsUpdated");

        return true;
    }

    public async Task ReorderAsync(IEnumerable<ReorderBacklogItemDto> dtos, string updatedBy)
    {
        var items = await _repository.GetAllAsync();
        var itemDict = items.ToDictionary(i => i.Id);

        var updatedItems = new List<BacklogItem>();
        foreach (var dto in dtos)
        {
            if (itemDict.TryGetValue(dto.Id, out var item))
            {
                if (item.ColumnId != dto.ColumnId)
                {
                    item.History.Add(new BacklogItemHistory
                    {
                        ChangeType = "Moved",
                        Description = $"Premaknjen v stolpec ID {dto.ColumnId}",
                        ChangedBy = updatedBy
                    });
                }
                item.Order = dto.Order;
                item.ColumnId = dto.ColumnId; // Update column during reorder
                updatedItems.Add(item);
            }
        }

        await _repository.UpdateOrdersAsync(updatedItems);

        await _hubContext.Clients.All.SendAsync("ItemsUpdated");
    }

    private static BacklogItemDto MapToDto(BacklogItem item) => new(
        item.Id,
        item.Title,
        item.Description,
        item.ColumnId,
        item.BoardId ?? 0,
        item.Priority,
        item.Order,
        item.CreatedAt,
        item.UpdatedAt,
        item.SprintId,
        item.Sprint?.Name,
        item.CreatedBy,
        item.AssignedTo,
        item.DueDate,
        item.Comments?.Count ?? 0,
        item.ParentId,
        item.Subtasks?.Select(MapToDto),
        item.Attachments?.Select(a => new AttachmentDto(a.Id, a.BacklogItemId, a.FileName, a.ContentType, a.Size, a.CreatedAt)),
        item.History?.OrderByDescending(h => h.ChangedAt).Select(h => new HistoryDto(h.Id, h.BacklogItemId, h.ChangeType, h.Description, h.ChangedBy, h.ChangedAt))
    );
}

public class AttachmentService : IAttachmentService
{
    private readonly BacklogDbContext _context;
    private readonly string _uploadPath;

    public AttachmentService(BacklogDbContext context, IConfiguration configuration)
    {
        _context = context;
        _uploadPath = configuration["UploadsPath"] ?? "Uploads";
        if (!Directory.Exists(_uploadPath)) Directory.CreateDirectory(_uploadPath);
    }

    public async Task<AttachmentDto> UploadAsync(int backlogItemId, string fileName, string contentType, Stream content)
    {
        var fileId = Guid.NewGuid().ToString();
        var filePath = Path.Combine(_uploadPath, fileId + Path.GetExtension(fileName));

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await content.CopyToAsync(fileStream);
        }

        var attachment = new Attachment
        {
            BacklogItemId = backlogItemId,
            FileName = fileName,
            FilePath = filePath,
            ContentType = contentType,
            Size = content.Length
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();

        return new AttachmentDto(attachment.Id, attachment.BacklogItemId, attachment.FileName, attachment.ContentType, attachment.Size, attachment.CreatedAt);
    }

    public async Task<Attachment?> GetByIdAsync(int id)
    {
        return await _context.Attachments.FindAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var attachment = await _context.Attachments.FindAsync(id);
        if (attachment == null) return false;

        if (File.Exists(attachment.FilePath))
        {
            File.Delete(attachment.FilePath);
        }

        _context.Attachments.Remove(attachment);
        await _context.SaveChangesAsync();
        return true;
    }
}

public class ColumnService : IColumnService
{
    private readonly IColumnRepository _repository;

    public ColumnService(IColumnRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BoardColumnDto>> GetAllAsync(int? boardId = null)
    {
        var columns = await _repository.GetAllAsync(boardId);
        return columns.Select(MapToDto);
    }

    public async Task<BoardColumnDto?> GetByIdAsync(int id)
    {
        var column = await _repository.GetByIdAsync(id);
        return column == null ? null : MapToDto(column);
    }

    public async Task<BoardColumnDto> CreateAsync(CreateBoardColumnDto dto)
    {
        var column = new BoardColumn
        {
            Name = dto.Name,
            Order = dto.Order,
            Color = dto.Color,
            BoardId = dto.BoardId
        };
        await _repository.AddAsync(column);
        await _repository.SaveChangesAsync();
        return MapToDto(column);
    }

    public async Task<bool> UpdateAsync(int id, CreateBoardColumnDto dto)
    {
        var column = await _repository.GetByIdAsync(id);
        if (column == null) return false;

        column.Name = dto.Name;
        column.Order = dto.Order;
        column.Color = dto.Color;
        column.BoardId = dto.BoardId;

        _repository.Update(column);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var column = await _repository.GetByIdAsync(id);
        if (column == null) return false;

        _repository.Delete(column);
        await _repository.SaveChangesAsync();
        return true;
    }

    private static BoardColumnDto MapToDto(BoardColumn c) => new(c.Id, c.Name, c.Order, c.Color, c.BoardId);
}

public class SprintService : ISprintService
{
    private readonly ISprintRepository _repository;
    private readonly IHubContext<Infrastructure.Hubs.BacklogHub> _hubContext;

    public SprintService(ISprintRepository repository, IHubContext<Infrastructure.Hubs.BacklogHub> hubContext)
    {
        _repository = repository;
        _hubContext = hubContext;
    }

    public async Task<IEnumerable<SprintDto>> GetAllAsync(int? boardId = null)
    {
        var sprints = await _repository.GetAllAsync(boardId);
        return sprints.Select(MapToDto);
    }

    public async Task<SprintDto?> GetByIdAsync(int id)
    {
        var sprint = await _repository.GetByIdAsync(id);
        return sprint == null ? null : MapToDto(sprint);
    }

    public async Task<SprintDto> CreateAsync(CreateSprintDto dto)
    {
        var sprint = new Sprint
        {
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive,
            BoardId = dto.BoardId
        };

        await _repository.AddAsync(sprint);
        await _repository.SaveChangesAsync();
        await _hubContext.Clients.All.SendAsync("SprintsUpdated");
        return MapToDto(sprint);
    }

    public async Task<bool> UpdateAsync(int id, CreateSprintDto dto)
    {
        var sprint = await _repository.GetByIdAsync(id);
        if (sprint == null) return false;

        sprint.Name = dto.Name;
        sprint.StartDate = dto.StartDate;
        sprint.EndDate = dto.EndDate;
        sprint.IsActive = dto.IsActive;
        sprint.BoardId = dto.BoardId;

        _repository.Update(sprint);
        await _repository.SaveChangesAsync();
        await _hubContext.Clients.All.SendAsync("SprintsUpdated");
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sprint = await _repository.GetByIdAsync(id);
        if (sprint == null) return false;

        _repository.Delete(sprint);
        await _repository.SaveChangesAsync();
        await _hubContext.Clients.All.SendAsync("SprintsUpdated");
        return true;
    }

    private static SprintDto MapToDto(Sprint s) => new(s.Id, s.Name, s.StartDate, s.EndDate, s.IsActive, s.BoardId);
}

public class CommentService : ICommentService
{
    private readonly ICommentRepository _repository;
    private readonly IHubContext<BacklogHub> _hubContext;

    public CommentService(ICommentRepository repository, IHubContext<BacklogHub> hubContext)
    {
        _repository = repository;
        _hubContext = hubContext;
    }

    public async Task<IEnumerable<CommentDto>> GetByBacklogItemIdAsync(int backlogItemId)
    {
        var comments = await _repository.GetByBacklogItemIdAsync(backlogItemId);
        return comments.Select(c => new CommentDto(c.Id, c.BacklogItemId, c.Content, c.Author, c.CreatedAt));
    }

    public async Task<CommentDto> AddCommentAsync(int backlogItemId, string content, string author)
    {
        var comment = new Comment
        {
            BacklogItemId = backlogItemId,
            Content = content,
            Author = author,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(comment);
        await _repository.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("ItemsUpdated");

        return new CommentDto(comment.Id, comment.BacklogItemId, comment.Content, comment.Author, comment.CreatedAt);
    }

    public async Task<bool> UpdateCommentAsync(int commentId, string content, string author)
    {
        var comment = await _repository.GetByIdAsync(commentId);
        if (comment == null) return false;

        if (comment.Author != author) return false;

        comment.Content = content;
        _repository.Update(comment);
        await _repository.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("ItemsUpdated");

        return true;
    }

    public async Task<bool> DeleteCommentAsync(int commentId, string author)
    {
        var comment = await _repository.GetByIdAsync(commentId);
        if (comment == null) return false;

        if (comment.Author != author) return false;

        _repository.Delete(comment);
        await _repository.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("ItemsUpdated");

        return true;
    }
}

public class BoardService : IBoardService
{
    private readonly IBoardRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;

    public BoardService(IBoardRepository repository, UserManager<ApplicationUser> userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    public async Task<IEnumerable<BoardDto>> GetAllAsync()
    {
        var boards = await _repository.GetAllAsync();
        return boards.Select(MapToDto);
    }

    public async Task<IEnumerable<BoardDto>> GetByUsernameAsync(string username)
    {
        var boards = await _repository.GetByUsernameAsync(username);
        return boards.Select(MapToDto);
    }

    public async Task<BoardDto?> GetByIdAsync(int id)
    {
        var board = await _repository.GetByIdAsync(id);
        return board == null ? null : MapToDto(board);
    }

    public async Task<BoardDto> CreateAsync(CreateBoardDto dto)
    {
        var board = new Board
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.Usernames != null)
        {
            foreach (var username in dto.Usernames)
            {
                var user = await _userManager.FindByNameAsync(username);
                if (user != null) board.Users.Add(user);
            }
        }

        await _repository.AddAsync(board);
        await _repository.SaveChangesAsync();

        var columnsToCreate = new List<BoardColumn>();
        if (dto.Columns != null && dto.Columns.Any())
        {
            foreach (var colDto in dto.Columns)
            {
                columnsToCreate.Add(new BoardColumn 
                { 
                    Name = colDto.Name, 
                    Order = colDto.Order, 
                    Color = colDto.Color, 
                    BoardId = board.Id 
                });
            }
        }
        else
        {
            columnsToCreate.Add(new BoardColumn { Name = "TODO", Order = 1, Color = "#cbd5e1", BoardId = board.Id });
            columnsToCreate.Add(new BoardColumn { Name = "In Progress", Order = 2, Color = "#6366f1", BoardId = board.Id });
            columnsToCreate.Add(new BoardColumn { Name = "Done", Order = 3, Color = "#22c55e", BoardId = board.Id });
        }

        foreach (var col in columnsToCreate)
        {
            _repository.AddColumn(col);
        }
        await _repository.SaveChangesAsync();
        
        return MapToDto(board);
    }

    public async Task<bool> UpdateAsync(int id, CreateBoardDto dto)
    {
        var board = await _repository.GetByIdAsync(id);
        if (board == null) return false;

        board.Name = dto.Name;
        board.Description = dto.Description;

        if (dto.Usernames != null)
        {
            board.Users.Clear();
            foreach (var username in dto.Usernames)
            {
                var user = await _userManager.FindByNameAsync(username);
                if (user != null) board.Users.Add(user);
            }
        }

        if (dto.Columns != null)
        {
            var currentColumns = board.Columns.ToList();
            var dtoColumnIds = dto.Columns.Where(c => c.Id.HasValue).Select(c => c.Id!.Value).ToList();

            foreach (var currentCol in currentColumns)
            {
                if (!dtoColumnIds.Contains(currentCol.Id))
                {
                    board.Columns.Remove(currentCol);
                }
            }

            foreach (var colDto in dto.Columns)
            {
                if (colDto.Id.HasValue)
                {
                    var existingCol = board.Columns.FirstOrDefault(c => c.Id == colDto.Id.Value);
                    if (existingCol != null)
                    {
                        existingCol.Name = colDto.Name;
                        existingCol.Order = colDto.Order;
                        existingCol.Color = colDto.Color;
                    }
                }
                else
                {
                    board.Columns.Add(new BoardColumn
                    {
                        Name = colDto.Name,
                        Order = colDto.Order,
                        Color = colDto.Color,
                        BoardId = board.Id
                    });
                }
            }
        }

        _repository.Update(board);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var board = await _repository.GetByIdAsync(id);
        if (board == null) return false;

        _repository.Delete(board);
        await _repository.SaveChangesAsync();
        return true;
    }

    private static BoardDto MapToDto(Board b) => new(
        b.Id,
        b.Name,
        b.Description,
        b.CreatedAt,
        b.Users.Select(u => u.UserName!),
        b.Columns.OrderBy(c => c.Order).Select(c => new BoardColumnDto(c.Id, c.Name, c.Order, c.Color, c.BoardId))
    );
}

public class NotificationService : INotificationService
{
    private readonly BacklogDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<BacklogHub> _hubContext;

    public NotificationService(BacklogDbContext context, UserManager<ApplicationUser> userManager, IHubContext<BacklogHub> hubContext)
    {
        _context = context;
        _userManager = userManager;
        _hubContext = hubContext;
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return Enumerable.Empty<NotificationDto>();

        var notifications = await _context.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        return notifications.Select(n => new NotificationDto(
            n.Id, n.Title, n.Message, n.Link, n.IsRead, n.CreatedAt, n.Type));
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return false;

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == user.Id);

        if (notification == null) return false;

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return false;

        var notifications = await _context.Notifications
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .ToListAsync();

        foreach (var n in notifications) n.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task CreateNotificationAsync(string username, string title, string message, string? link = null, string? type = null)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return;

        var notification = new Notification
        {
            UserId = user.Id,
            Title = title,
            Message = message,
            Link = link,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Notify user via SignalR if they are online
        await _hubContext.Clients.User(user.Id).SendAsync("ReceiveNotification", new NotificationDto(
            notification.Id, notification.Title, notification.Message, notification.Link, notification.IsRead, notification.CreatedAt, notification.Type));
    }
}
public class SmtpSettingsService : ISmtpSettingsService
{
    private readonly BacklogDbContext _context;
    private readonly ILogger<SmtpSettingsService> _logger;

    public SmtpSettingsService(BacklogDbContext context, ILogger<SmtpSettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SmtpSettingsDto?> GetSettingsAsync()
    {
        var settings = await _context.SmtpSettings.FirstOrDefaultAsync();
        if (settings == null) return null;
        
        return new SmtpSettingsDto(
            settings.Id,
            settings.Host,
            settings.Port,
            settings.UserName,
            null, // Don't return password
            settings.EnableSsl,
            settings.FromEmail,
            settings.FromName,
            settings.UpdatedAt
        );
    }

    public async Task<bool> UpdateSettingsAsync(UpdateSmtpSettingsDto dto)
    {
        var settings = await _context.SmtpSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new SmtpSettings();
            _context.SmtpSettings.Add(settings);
        }

        settings.Host = dto.Host;
        settings.Port = dto.Port;
        settings.UserName = dto.UserName;
        if (!string.IsNullOrEmpty(dto.Password))
        {
            settings.Password = dto.Password;
        }
        settings.EnableSsl = dto.EnableSsl;
        settings.FromEmail = dto.FromEmail;
        settings.FromName = dto.FromName;
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        var settings = await _context.SmtpSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrEmpty(settings.Host))
        {
            _logger.LogWarning("SMTP settings are not configured.");
            return false;
        }

        try
        {
            using var client = new System.Net.Mail.SmtpClient(settings.Host, settings.Port);
            client.EnableSsl = settings.EnableSsl;
            client.Credentials = new System.Net.NetworkCredential(settings.UserName, settings.Password);
            client.Timeout = 10000; // 10 seconds timeout to prevent gateway timeouts

            // Force STARTTLS on port 587 or 2525 if SSL is enabled
            if (settings.EnableSsl && (settings.Port == 587 || settings.Port == 2525 || settings.Port == 25))
            {
                // SmtpClient.EnableSsl = true usually triggers STARTTLS on these ports
            }

            var mailMessage = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(settings.FromEmail, settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            _logger.LogInformation("Sending email to {To} via {Host}:{Port} (SSL: {EnableSsl})", to, settings.Host, settings.Port, settings.EnableSsl);
            _logger.LogDebug("SMTP Debug: User={UserName}, From={FromEmail}, Ssl={EnableSsl}, Timeout={Timeout}", settings.UserName, settings.FromEmail, client.EnableSsl, client.Timeout);
            
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} via {Host}:{Port}. Error: {Message}. Inner Error: {InnerMessage}", to, settings.Host, settings.Port, ex.Message, ex.InnerException?.Message);
            return false;
        }
    }
}
