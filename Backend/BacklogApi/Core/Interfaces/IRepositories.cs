using BacklogApi.Core.Entities;

namespace BacklogApi.Core.Interfaces;

public interface IBacklogRepository
{
    Task<IEnumerable<BacklogItem>> GetAllAsync(int? boardId = null, bool personal = false, string? username = null);
    Task<BacklogItem?> GetByIdAsync(int id);
    Task AddAsync(BacklogItem item);
    void Update(BacklogItem item);
    void Delete(BacklogItem item);
    Task SaveChangesAsync();
    Task<int> GetNextOrderAsync();
    Task UpdateOrdersAsync(IEnumerable<BacklogItem> items);
}

public interface ISprintRepository
{
    Task<IEnumerable<Sprint>> GetAllAsync(int? boardId = null);
    Task<Sprint?> GetByIdAsync(int id);
    Task AddAsync(Sprint sprint);
    void Update(Sprint sprint);
    void Delete(Sprint sprint);
    Task SaveChangesAsync();
}

public interface IColumnRepository
{
    Task<IEnumerable<BoardColumn>> GetAllAsync(int? boardId = null);
    Task<BoardColumn?> GetByIdAsync(int id);
    Task AddAsync(BoardColumn column);
    void Update(BoardColumn column);
    void Delete(BoardColumn column);
    Task SaveChangesAsync();
}

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetByBacklogItemIdAsync(int backlogItemId);
    Task AddAsync(Comment comment);
    Task<Comment?> GetByIdAsync(int id);
    void Update(Comment comment);
    void Delete(Comment comment);
    Task SaveChangesAsync();
}

public interface IBoardRepository
{
    Task<IEnumerable<Board>> GetAllAsync();
    Task<IEnumerable<Board>> GetByUsernameAsync(string username);
    Task<Board?> GetByIdAsync(int id);
    Task AddAsync(Board board);
    void Update(Board board);
    void Delete(Board board);
    Task SaveChangesAsync();
    void AddColumn(BoardColumn column);
}

public interface INoteRepository
{
    Task<IEnumerable<Note>> GetAllByUsernameAsync(string username);
    Task<Note?> GetByIdAsync(int id);
    Task AddAsync(Note note);
    void Update(Note note);
    void Delete(Note note);
    Task SaveChangesAsync();
}
