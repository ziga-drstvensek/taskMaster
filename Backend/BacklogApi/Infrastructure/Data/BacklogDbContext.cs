using BacklogApi.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BacklogApi.Infrastructure.Data;

public class BacklogDbContext : IdentityDbContext<ApplicationUser>
{
    public BacklogDbContext(DbContextOptions<BacklogDbContext> options) : base(options)
    {
    }

    public DbSet<BacklogItem> BacklogItems { get; set; }
    public DbSet<BoardColumn> Columns { get; set; }
    public DbSet<Sprint> Sprints { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Board> Boards { get; set; }
    public DbSet<BacklogItemHistory> History { get; set; }
    public DbSet<SmtpSettings> SmtpSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BacklogItemHistory>()
            .HasOne(h => h.BacklogItem)
            .WithMany(b => b.History)
            .HasForeignKey(h => h.BacklogItemId);

        modelBuilder.Entity<BacklogItem>()
            .HasOne(b => b.Column)
            .WithMany(c => c.BacklogItems)
            .HasForeignKey(b => b.ColumnId);

        modelBuilder.Entity<BacklogItem>()
            .HasOne(b => b.Board)
            .WithMany(b => b.BacklogItems)
            .HasForeignKey(b => b.BoardId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<BoardColumn>()
            .HasOne(c => c.Board)
            .WithMany(b => b.Columns)
            .HasForeignKey(c => c.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Board>()
            .HasMany(b => b.Users)
            .WithMany();

        modelBuilder.Entity<Sprint>()
            .HasOne(s => s.Board)
            .WithMany(b => b.Sprints)
            .HasForeignKey(s => s.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BacklogItem>()
            .HasOne(b => b.Sprint)
            .WithMany(s => s.BacklogItems)
            .HasForeignKey(b => b.SprintId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.BacklogItem)
            .WithMany(b => b.Comments)
            .HasForeignKey(c => c.BacklogItemId);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.BacklogItem)
            .WithMany(b => b.Attachments)
            .HasForeignKey(a => a.BacklogItemId);

        modelBuilder.Entity<BacklogItem>()
            .HasOne(b => b.Parent)
            .WithMany(b => b.Subtasks)
            .HasForeignKey(b => b.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
