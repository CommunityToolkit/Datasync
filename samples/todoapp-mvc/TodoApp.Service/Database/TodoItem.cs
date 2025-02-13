using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TodoApp.Service.Database;

public class TodoItem : EntityTableData
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
    public bool Completed { get; set; }
}

