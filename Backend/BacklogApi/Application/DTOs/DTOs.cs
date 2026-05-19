using BacklogApi.Core.Enums;

namespace BacklogApi.Application.DTOs;

public record BacklogItemDto(
    int Id,
    string Title,
    string? Description,
    int? ColumnId,
    int? BoardId,
    BacklogItemPriority Priority,
    int Order,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int? SprintId,
    string? SprintName,
    string CreatedBy,
    string? AssignedTo,
    DateTime? DueDate = null,
    int CommentsCount = 0,
    int? ParentId = null,
    IEnumerable<BacklogItemDto>? Subtasks = null,
    IEnumerable<AttachmentDto>? Attachments = null,
    IEnumerable<HistoryDto>? History = null
);

public record HistoryDto(
    int Id,
    int BacklogItemId,
    string ChangeType,
    string? Description,
    string ChangedBy,
    DateTime ChangedAt
);

public record AttachmentDto(
    int Id,
    int BacklogItemId,
    string FileName,
    string ContentType,
    long Size,
    DateTime CreatedAt
);

public record BoardDto(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    IEnumerable<string> Usernames,
    IEnumerable<BoardColumnDto> Columns
);

public record CreateBoardDto(
    string Name,
    string? Description,
    IEnumerable<string>? Usernames = null,
    IEnumerable<CreateBoardColumnDto>? Columns = null
);

public record CreateBacklogItemDto(
    string Title,
    string? Description,
    int? ColumnId,
    int? BoardId,
    BacklogItemPriority Priority,
    int? SprintId,
    string? AssignedTo,
    DateTime? DueDate = null,
    int? ParentId = null
);

public record UpdateBacklogItemDto(
    string Title,
    string? Description,
    int? ColumnId,
    int? BoardId,
    BacklogItemPriority Priority,
    int? SprintId,
    string? AssignedTo,
    DateTime? DueDate = null,
    int? ParentId = null
);

public record ReorderBacklogItemDto(
    int Id,
    int Order,
    int ColumnId
);

public record BoardColumnDto(
    int Id,
    string Name,
    int Order,
    string Color,
    int? BoardId
);

public record CreateBoardColumnDto(
    string Name,
    int Order,
    string Color,
    int? BoardId,
    int? Id = null
);

public record SprintDto(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive,
    int? BoardId
);

public record CreateSprintDto(
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive,
    int? BoardId
);

public record CommentDto(
    int Id,
    int BacklogItemId,
    string Content,
    string Author,
    DateTime CreatedAt
);

public record CreateCommentDto(
    string Content
);

public record LoginDto(string Username, string Password);
public record RegisterDto(string Username, string Email, string Password);
public record UpdateUserDto(string Username, string Email, string? Password, string Role, string? Tags, string? ProfilePicture = null, string? TeamsWebhookUrl = null);
public record UpdateProfileDto(string? ProfilePicture, string? TeamsWebhookUrl = null);
public record UserDto(string Username, string Email, string Role, string? Tags, string? ProfilePicture, string? TeamsWebhookUrl = null);
public record AuthResponseDto(string Token, string Username, string Role, string? ProfilePicture = null, string? TeamsWebhookUrl = null);

public record NotificationDto(
    int Id,
    string Title,
    string Message,
    string? Link,
    bool IsRead,
    DateTime CreatedAt,
    string? Type
);

public record SmtpSettingsDto(
    int Id,
    string Host,
    int Port,
    string UserName,
    string? Password,
    bool EnableSsl,
    string FromEmail,
    string FromName,
    DateTime UpdatedAt
);

public record UpdateSmtpSettingsDto(
    string Host,
    int Port,
    string UserName,
    string? Password,
    bool EnableSsl,
    string FromEmail,
    string FromName
);

public record NoteDto(
    int Id,
    string Title,
    string Content,
    string Username,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int Order
);

public record CreateNoteDto(
    string Title,
    string Content
);

public record UpdateNoteDto(
    string Title,
    string Content
);
