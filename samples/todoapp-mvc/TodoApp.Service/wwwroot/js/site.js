$(function () {
    'use strict';

    Handlebars.registerHelper('eq', function (a, b, options) {
        return a === b ? options.fn(this) : options.inverse(this);
    });

    const DatasyncService = {
        fetchTodoItems: async () => {
            console.debug('DatasyncService.fetchTodoItems');
            const response = await fetch('/tables/todoitems');
            console.debug('DatasyncService.fetchTodoItems: response = ', response);
            if (response.ok) {
                const data = await response.json();
                console.debug('DatasyncService.fetchTodoItems: data = ', data);
                return data.items;
            }
            throw new Error(`Failed to fetch todo items: status=${response.status}`);
        },
        createTodoItem: async (title) => {
            console.debug('DatasyncService.createTodoItem: title = ', title);
            const response = await fetch('/tables/todoitems', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ title: title })
            });
            console.debug('DatasyncService.createTodoItem: response = ', response);
            if (response.ok) {
                const data = await response.json();
                console.debug('DatasyncService.createTodoItem: data = ', data);
                return data;
            }
            throw new Error(`Failed to create todo item: status=${response.status}`);
        },
        editTodoItem: async (todoItem) => {
            console.debug('DatasyncService.editTodoItem: todoItem = ', todoItem);
            const response = await fetch(`/tables/todoitems/${todoItem.id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(todoItem)
            });
            console.debug('DatasyncService.editTodoItem: response = ', response);
            if (response.ok) {
                const data = await response.json();
                console.debug('DatasyncService.editTodoItem: data = ', data);
                return data;
            }
            throw new Error(`Failed to edit todo item: status=${response.status}`);
        },
        removeTodoItem: async (todoItem) => {
            console.debug('DatasyncService.removeTodoItem: todoItem = ', todoItem);
            const response = await fetch(`/tables/todoitems/${todoItem.id}`, {
                method: 'DELETE'
            });
            console.debug('DatasyncService.removeTodoitem: response = ', response);
            if (response.status === 204) {
                return;
            }
            throw new Error(`Failed to remove todo item: status=${response.status}`);
        },
    };

    let todoItems = [];
    let listFilter = 'all';

    const todoTemplate = Handlebars.compile($('#todo-template').html());
    const footerTemplate = Handlebars.compile($('#footer-template').html());

    const pluralize = function (count, word) {
        return count === 1 ? word : word + 's';
    };

    const indexFromEl = (el) => {
        const id = $(el).closest('li').data('id');
        return todoItems.findIndex(item => item.id === id);
    };

    const getFilteredTodoItems = () => {
        if (listFilter === 'active') {
            return todoItems.filter(item => !item.completed);
        }

        if (listFilter === 'completed') {
            return todoItems.filter(item => item.completed);
        }

        return todoItems;
    };

    const renderTodoItems = () => {
        const itemsToRender = getFilteredTodoItems();
        const activeCount = todoItems.filter(item => !item.completed).length;
        const completedCount = todoItems.length - activeCount;

        $('#todo-list').html(todoTemplate(itemsToRender));
        $('#main').toggle(itemsToRender.length > 0);
        $('#toggle-all').prop('checked', activeCount === 0);
        $('#new-todo').trigger('focus');

        $('#footer').toggle(todoItems.length > 0).html(footerTemplate({
            activeTodoCount: activeCount,
            activeTodoWord: pluralize(activeCount, 'item'),
            completedTodos: completedCount,
            filter: listFilter
        }));
    };

    const createTodoItem = async (title) => {
        console.debug('createTodoItem: title = ', title);
        let newItem = await DatasyncService.createTodoItem(title);
        todoItems.push(newItem);
    }

    const editTodoItem = async (todoItem, changes) => {
        console.debug('editTodoItem: todoItem = ', todoItem, 'changes = ', changes);
        const idx = todoItems.findIndex(item => item.id == todoItem.id);
        console.debug('editTodoItem: idx = ', idx);
        if (idx >= 0) {
            const todoItemWithEdits = Object.assign({}, todoItems[idx], changes);
            todoItems[idx] = await DatasyncService.editTodoItem(todoItemWithEdits);
        }
    };

    const removeTodoItem = async (todoItem) => {
        await DatasyncService.removeTodoItem(todoItem);
        todoItems = todoItems.filter(item => item.id !== todoItem.id);
        console.debug('removeTodoItem: ', todoItem);
    };

    $('#new-todo').on('keyup', async (e) => {
        const $input = $(e.target), val = $input.val().trim();
        if (e.which !== 13 || !val) return;

        await createTodoItem(val);
        $input.val('');
        renderTodoItems();
    });

    $('#toggle-all').on('change', async (e) => {
        const isChecked = $(e.target).prop('checked');
        for (const todoItem of todoItems) {
            if (todoItem.completed !== isChecked) {
                await editTodoItem(todoItem, { completed: isChecked });
            }
        }
        renderTodoItems();
    });

    $('#footer').on('click', '.clear-completed', async (e) => {
        for (const todoItem of todoItems) {
            if (todoItem.completed) {
                await removeTodoItem(todoItem);
            }
        }
        listFilter = 'all';
        renderTodoItems();
    });

    $('#todo-list').on('change', '.toggle', async (e) => {
        const idx = indexFromEl(e.target);
        await editTodoItem(todoItems[idx], { completed: !todoItems[idx].completed });
        renderTodoItems();
    });

    $('#todo-list').on('dblclick', 'label', (e) => {
        const $input = $(e.target).closest('li').addClass('editing').find('.edit');
        const title = $(e.target).text();
        $input.trigger("focus").val("").val(title);
    });

    $('#todo-list').on('keyup', '.edit', (e) => {
        if (e.which === 13) {
            e.target.blur();
        }

        if (e.which === 27) {
            $(e.target).data('abort', true).trigger('blur');
        }
    });

    $('#todo-list').on('focusout', '.edit', async (e) => {
        const el = e.target, $el = $(el), val = $el.val().trim();

        if (!val) {
            await removeTodoItem(todoItems[indexFromEl(el)]);
            renderTodoItems();
            return;
        }

        if ($el.data('abort')) {
            $el.data('abort', false);
        } else {
            await editTodoItem(todoItems[indexFromEl(el)], { title: val });
        }

        renderTodoItems();
    });

    $('#todo-list').on('click', '.destroy', async (e) => {
        await removeTodoItem(todoItems[indexFromEl(el)]);
        renderTodoItems();
    });

    const router = new Router({
        '/:filter': (filter) => {
            listFilter = filter;
            renderTodoItems();
        }
    }).init('/all');

    // Fetch the items from the service.
    DatasyncService.fetchTodoItems().then(items => {
        todoItems = items;
        renderTodoItems();
    }).catch(err => {
        console.error(err);
    });
});