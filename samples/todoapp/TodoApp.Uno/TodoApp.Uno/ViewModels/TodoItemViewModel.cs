using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoApp.Uno.Database;

namespace TodoApp.Uno.ViewModels
{
    public partial class TodoItemViewModel(TodoItem todoItem) : ObservableObject
    {
        public readonly TodoItem _todoItem = todoItem;

        public string Title
        {
            get => _todoItem.Title;
            set => SetProperty(_todoItem.Title, value, _todoItem, (item, value) => item.Title = value);
        }

        public bool IsComplete
        {
            get => _todoItem.IsComplete;
            set => SetProperty(_todoItem.IsComplete, value, _todoItem, (item, value) => item.IsComplete = value);
        }

        public string Version
        {
            get => _todoItem.Version;
            set => SetProperty(_todoItem.Version, value, _todoItem, (item, value) => item.Version = value);
        }

        public string Id => _todoItem.Id;

        public DateTimeOffset? UpdatedAt
        {
            get => _todoItem.UpdatedAt;
            set => SetProperty(_todoItem.UpdatedAt, value, _todoItem, (item, value) => item.UpdatedAt = value);
        }

        public bool Deleted
        {
            get => _todoItem.Deleted;
            set => SetProperty(_todoItem.Deleted, value, _todoItem, (item, value) => item.Deleted = value);
        }
    }
}
