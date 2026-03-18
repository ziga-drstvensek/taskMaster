using BacklogApi.Core.Entities;
using BacklogApi.Core.Interfaces;
using BacklogApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BacklogApi.Infrastructure.Repositories;

public class BacklogRepository : IBacklogRepository
{
    private readonly BacklogDbContext _context;

    public BacklogRepository(BacklogDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BacklogItem>> GetAllAsync(int? boardId = null)
    {
        var query = _context.BacklogItems
            .Include(b => b.Sprint)
            .Include(b => b.Column)
            .Include(b => b.Comments)
            .Include(b => b.Subtasks)
                .ThenInclude(s => s.Comments)
            .Include(b => b.Attachments)
            .Include(b => b.History)
            .AsQueryable();

        if (boardId == 0)
        {
            query = query.Where(b => b.BoardId == 0);
        }
        else if (boardId.HasValue)
        {
            query = query.Where(b => b.BoardId == boardId.Value);
        }

        return await query
            .OrderBy(b => b.Order)
            .ToListAsync();
    }

    public async Task<BacklogItem?> GetByIdAsync(int id)
    {
        return await _context.BacklogItems
            .Include(b => b.Sprint)
            .Include(b => b.Column)
            .Include(b => b.Comments)
            .Include(b => b.Subtasks)
                .ThenInclude(s => s.Comments)
            .Include(b => b.Attachments)
            .Include(b => b.History)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task AddAsync(BacklogItem item)
    {
        await _context.BacklogItems.AddAsync(item);
    }

    public void Update(BacklogItem item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _context.BacklogItems.Update(item);
    }

    public void Delete(BacklogItem item)
    {
        _context.BacklogItems.Remove(item);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetNextOrderAsync()
    {
        var maxOrder = await _context.BacklogItems.MaxAsync(b => (int?)b.Order) ?? 0;
        return maxOrder + 1;
    }

    public async Task UpdateOrdersAsync(IEnumerable<BacklogItem> items)
    {
        foreach (var item in items)
        {
            _context.Entry(item).Property(x => x.Order).IsModified = true;
            _context.Entry(item).Property(x => x.ColumnId).IsModified = true;
            item.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }
}

public class SprintRepository : ISprintRepository
{
    private readonly BacklogDbContext _context;

    public SprintRepository(BacklogDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Sprint>> GetAllAsync(int? boardId = null)
    {
        var query = _context.Sprints.AsQueryable();
        if (boardId.HasValue)
        {
            query = query.Where(s => s.BoardId == boardId.Value);
        }
        return await query.ToListAsync();
    }

    public async Task<Sprint?> GetByIdAsync(int id)
    {
        return await _context.Sprints
            .Include(s => s.BacklogItems)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddAsync(Sprint sprint)
    {
        await _context.Sprints.AddAsync(sprint);
    }

    public void Update(Sprint sprint)
    {
        _context.Sprints.Update(sprint);
    }

    public void Delete(Sprint sprint)
    {
        _context.Sprints.Remove(sprint);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

public class ColumnRepository : IColumnRepository
{
    private readonly BacklogDbContext _context;

    public ColumnRepository(BacklogDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BoardColumn>> GetAllAsync(int? boardId = null)
    {
        var query = _context.Columns.AsQueryable();

        if (boardId.HasValue && boardId.Value != 0 && boardId.Value != -1)
        {
            query = query.Where(c => c.BoardId == boardId.Value);
        }
        else
        {
            query = query.Where(c => c.BoardId == null);
        }

        return await query
            .OrderBy(c => c.Order)
            .ToListAsync();
    }

    public async Task<BoardColumn?> GetByIdAsync(int id)
    {
        return await _context.Columns.FindAsync(id);
    }

    public async Task AddAsync(BoardColumn column)
    {
        await _context.Columns.AddAsync(column);
    }

    public void Update(BoardColumn column)
    {
        _context.Columns.Update(column);
    }

    public void Delete(BoardColumn column)
    {
        _context.Columns.Remove(column);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

public class CommentRepository : ICommentRepository
{
    private readonly BacklogDbContext _context;

    public CommentRepository(BacklogDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Comment>> GetByBacklogItemIdAsync(int backlogItemId)
    {
        return await _context.Comments
            .Where(c => c.BacklogItemId == backlogItemId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Comment comment)
    {
        await _context.Comments.AddAsync(comment);
    }

    public async Task<Comment?> GetByIdAsync(int id)
    {
        return await _context.Comments.FindAsync(id);
    }

    public void Update(Comment comment)
    {
        _context.Comments.Update(comment);
    }

    public void Delete(Comment comment)
    {
        _context.Comments.Remove(comment);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

public class BoardRepository : IBoardRepository
{
    private readonly BacklogDbContext _context;

    public BoardRepository(BacklogDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Board>> GetAllAsync()
    {
        return await _context.Boards
            .Include(b => b.Users)
            .Include(b => b.Columns)
            .ToListAsync();
    }

    public async Task<IEnumerable<Board>> GetByUsernameAsync(string username)
    {
        return await _context.Boards
            .Include(b => b.Users)
            .Include(b => b.Columns)
            .Where(b => b.Users.Any(u => u.UserName == username))
            .ToListAsync();
    }

    public async Task<Board?> GetByIdAsync(int id)
    {
        return await _context.Boards
            .Include(b => b.Users)
            .Include(b => b.Columns)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task AddAsync(Board board)
    {
        await _context.Boards.AddAsync(board);
    }

    public void Update(Board board)
    {
        _context.Boards.Update(board);
    }

    public void Delete(Board board)
    {
        _context.Boards.Remove(board);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public void AddColumn(BoardColumn column)
    {
        _context.Columns.Add(column);
    }
}
