namespace ClientApp.Models;

public class TodoItem : OfflineClientEntity, IEquatable<TodoItem>
{
    public string Title { get; set; } = string.Empty;
    public bool IsComplete { get; set; }

    public bool Equals(TodoItem? other)
        => other is not null && Id == other.Id && Version == other.Version;
}