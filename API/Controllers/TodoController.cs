using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WuanTech.API.DTOs;
using WuanTech.API.Services.Interfaces;

namespace WuanTech.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TodoController : ControllerBase
    {
        private readonly ITodoService _todoService;
        private readonly ILogger<TodoController> _logger;

        public TodoController(ITodoService todoService, ILogger<TodoController> logger)
        {
            _todoService = todoService;
            _logger = logger;
        }

        /// <summary>
        /// Get all todos for current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<TodoDto>>>> GetTodos()
        {
            try
            {
                var userId = GetCurrentUserId();
                var todos = await _todoService.GetUserTodosAsync(userId);

                return Ok(new ApiResponse<IEnumerable<TodoDto>>
                {
                    Success = true,
                    Data = todos,
                    Message = "Todos retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting todos");
                return StatusCode(500, new ApiResponse<IEnumerable<TodoDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving todos"
                });
            }
        }

        /// <summary>
        /// Create a new todo
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<TodoDto>>> CreateTodo([FromBody] CreateTodoDto createTodoDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<TodoDto>
                    {
                        Success = false,
                        Message = "Invalid todo data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var userId = GetCurrentUserId();
                var todo = await _todoService.CreateTodoAsync(userId, createTodoDto);

                return CreatedAtAction(nameof(GetTodos), new ApiResponse<TodoDto>
                {
                    Success = true,
                    Data = todo,
                    Message = "Todo created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating todo");
                return StatusCode(500, new ApiResponse<TodoDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the todo"
                });
            }
        }

        /// <summary>
        /// Update a todo
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateTodo(int id, [FromBody] UpdateTodoDto updateTodoDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid todo data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var userId = GetCurrentUserId();
                var result = await _todoService.UpdateTodoAsync(userId, id, updateTodoDto);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Todo not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Todo updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating todo: {TodoId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while updating the todo"
                });
            }
        }

        /// <summary>
        /// Delete a todo
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteTodo(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _todoService.DeleteTodoAsync(userId, id);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Todo not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Todo deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting todo: {TodoId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the todo"
                });
            }
        }

        /// <summary>
        /// Toggle todo completion status
        /// </summary>
        [HttpPatch("{id}/toggle")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleTodo(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _todoService.ToggleTodoAsync(userId, id);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Todo not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Todo status toggled successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling todo: {TodoId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while toggling the todo"
                });
            }
        }

        /// <summary>
        /// Get todo statistics for current user
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<TodoStatsDto>>> GetTodoStats()
        {
            try
            {
                var userId = GetCurrentUserId();
                var stats = await _todoService.GetTodoStatsAsync(userId);

                return Ok(new ApiResponse<TodoStatsDto>
                {
                    Success = true,
                    Data = stats,
                    Message = "Todo statistics retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting todo stats");
                return StatusCode(500, new ApiResponse<TodoStatsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving todo statistics"
                });
            }
        }

        /// <summary>
        /// Get overdue todos for current user
        /// </summary>
        [HttpGet("overdue")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TodoDto>>>> GetOverdueTodos()
        {
            try
            {
                var userId = GetCurrentUserId();
                var todos = await _todoService.GetOverdueTodosAsync(userId);

                return Ok(new ApiResponse<IEnumerable<TodoDto>>
                {
                    Success = true,
                    Data = todos,
                    Message = "Overdue todos retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue todos");
                return StatusCode(500, new ApiResponse<IEnumerable<TodoDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving overdue todos"
                });
            }
        }

        #region Private Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid user credentials");
            }
            return userId;
        }

        #endregion
    }
}