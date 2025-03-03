using CommunityToolkit.Datasync.Server.InMemory;
using System.ComponentModel.DataAnnotations;

namespace ServerApp.Models;

public class TodoItem : InMemoryTableData, IEquatable<TodoItem>
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    public bool IsComplete { get; set; }

    public bool Equals(TodoItem? other)
        => other is not null && Id == other.Id && Version.SequenceEqual(other.Version);
}