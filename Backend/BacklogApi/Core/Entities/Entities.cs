using BacklogApi.Core.Enums;

namespace BacklogApi.Core.Entities;

public class BacklogItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public int ColumnId { get; set; }
    public BoardColumn? Column { get; set; }

    public int? BoardId { get; set; }
    public Board? Board { get; set; }
    
    public BacklogItemPriority Priority { get; set; } = BacklogItemPriority.Medium;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int? SprintId { get; set; }
    public Sprint? Sprint { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public int? ParentId { get; set; }
    public BacklogItem? Parent { get; set; }
    public ICollection<BacklogItem> Subtasks { get; set; } = new List<BacklogItem>();
    public string CreatedBy { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<BacklogItemHistory> History { get; set; } = new List<BacklogItemHistory>();
}

public class BacklogItemHistory
{
    public int Id { get; set; }
    public int BacklogItemId { get; set; }
    public BacklogItem? BacklogItem { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

public class Board
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BacklogItem> BacklogItems { get; set; } = new List<BacklogItem>();
    public ICollection<BoardColumn> Columns { get; set; } = new List<BoardColumn>();
    public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}

public class Attachment
{
    public int Id { get; set; }
    public int BacklogItemId { get; set; }
    public BacklogItem? BacklogItem { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class BoardColumn
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Color { get; set; } = "#cbd5e1";

    public int? BoardId { get; set; }
    public Board? Board { get; set; }

    public ICollection<BacklogItem> BacklogItems { get; set; } = new List<BacklogItem>();
}

public class Sprint
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    public int? BoardId { get; set; }
    public Board? Board { get; set; }

    public ICollection<BacklogItem> BacklogItems { get; set; } = new List<BacklogItem>();
}

public class Comment
{
    public int Id { get; set; }
    public int BacklogItemId { get; set; }
    public BacklogItem? BacklogItem { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
