using WuanTech.API.Data;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;
using Microsoft.EntityFrameworkCore;
using WuanTech.API.DTOs;

namespace WuanTech.API.Services.Implementations
{
    public class TodoService : ITodoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TodoService> _logger;

        public TodoService(ApplicationDbContext context, ILogger<TodoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<TodoDto>> GetUserTodosAsync(int userId)
        {
            try
            {
                var todos = await _context.Todos
                    .Where(t => t.UserId == userId)
                    .OrderBy(t => t.IsCompleted)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return todos.Select(t => new TodoDto
                {
                    Id = t.Id,
                    Text = t.Text,
                    IsCompleted = t.IsCompleted,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt,
                    CompletedAt = t.CompletedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting todos for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<TodoDto> CreateTodoAsync(int userId, CreateTodoDto todoDto)
        {
            try
            {
                var todo = new Todo
                {
                    UserId = userId,
                    Text = todoDto.Text,
                    Priority = todoDto.Priority,
                    DueDate = todoDto.DueDate,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Todos.Add(todo);
                await _context.SaveChangesAsync();

                return new TodoDto
                {
                    Id = todo.Id,
                    Text = todo.Text,
                    IsCompleted = todo.IsCompleted,
                    Priority = todo.Priority,
                    DueDate = todo.DueDate,
                    CreatedAt = todo.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating todo");
                throw;
            }
        }

        public async Task<bool> UpdateTodoAsync(int userId, int todoId, UpdateTodoDto todoDto)
        {
            try
            {
                var todo = await _context.Todos
                    .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

                if (todo == null)
                    return false;

                todo.Text = todoDto.Text;
                todo.Priority = todoDto.Priority;
                todo.DueDate = todoDto.DueDate;

                if (todoDto.IsCompleted && !todo.IsCompleted)
                {
                    todo.IsCompleted = true;
                    todo.CompletedAt = DateTime.UtcNow;
                }
                else if (!todoDto.IsCompleted && todo.IsCompleted)
                {
                    todo.IsCompleted = false;
                    todo.CompletedAt = null;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating todo: {TodoId}", todoId);
                throw;
            }
        }

        public async Task<bool> DeleteTodoAsync(int userId, int todoId)
        {
            try
            {
                var todo = await _context.Todos
                    .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

                if (todo == null)
                    return false;

                _context.Todos.Remove(todo);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting todo: {TodoId}", todoId);
                throw;
            }
        }

        public async Task<bool> ToggleTodoAsync(int userId, int todoId)
        {
            try
            {
                var todo = await _context.Todos
                    .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

                if (todo == null)
                    return false;

                todo.IsCompleted = !todo.IsCompleted;
                todo.CompletedAt = todo.IsCompleted ? DateTime.UtcNow : null;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling todo: {TodoId}", todoId);
                throw;
            }
        }

        public async Task<TodoStatsDto> GetTodoStatsAsync(int userId)
        {
            try
            {
                var todos = await _context.Todos
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                var totalTodos = todos.Count;
                var completedTodos = todos.Count(t => t.IsCompleted);
                var pendingTodos = totalTodos - completedTodos;

                var highPriorityTodos = todos.Count(t => t.Priority == "High" && !t.IsCompleted);
                var mediumPriorityTodos = todos.Count(t => t.Priority == "Medium" && !t.IsCompleted);
                var lowPriorityTodos = todos.Count(t => t.Priority == "Low" && !t.IsCompleted);

                var overdueTodos = todos.Count(t => !t.IsCompleted &&
                                                   t.DueDate.HasValue &&
                                                   t.DueDate.Value < DateTime.UtcNow);

                var completionRate = totalTodos > 0 ? (double)completedTodos / totalTodos * 100 : 0;

                return new TodoStatsDto
                {
                    TotalTodos = totalTodos,
                    CompletedTodos = completedTodos,
                    PendingTodos = pendingTodos,
                    HighPriorityTodos = highPriorityTodos,
                    MediumPriorityTodos = mediumPriorityTodos,
                    LowPriorityTodos = lowPriorityTodos,
                    OverdueTodos = overdueTodos,
                    CompletionRate = Math.Round(completionRate, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting todo stats for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<TodoDto>> GetOverdueTodosAsync(int userId)
        {
            try
            {
                var now = DateTime.UtcNow;
                var overdueTodos = await _context.Todos
                    .Where(t => t.UserId == userId &&
                               !t.IsCompleted &&
                               t.DueDate.HasValue &&
                               t.DueDate.Value < now)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();

                return overdueTodos.Select(t => new TodoDto
                {
                    Id = t.Id,
                    Text = t.Text,
                    IsCompleted = t.IsCompleted,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt,
                    CompletedAt = t.CompletedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue todos for user: {UserId}", userId);
                throw;
            }
        }
    }
}