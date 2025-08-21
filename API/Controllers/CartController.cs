// CartController.cs - Fixed
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WuanTech.API.Services.Interfaces;
using WuanTech.API.DTOs;

namespace WuanTech.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user's cart
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<CartDto>>> GetCart()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var cart = await _cartService.GetCartAsync(userId);

                return Ok(new ApiResponse<CartDto>
                {
                    Success = true,
                    Data = cart,
                    Message = "Cart retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart");
                return StatusCode(500, new ApiResponse<CartDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the cart"
                });
            }
        }

        /// <summary>
        /// Add item to cart
        /// </summary>
        [HttpPost("add")]
        public async Task<ActionResult<ApiResponse<CartItemDto>>> AddToCart([FromBody] AddToCartDto addToCartDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<CartItemDto>
                    {
                        Success = false,
                        Message = "Invalid cart data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var cartItem = await _cartService.AddToCartAsync(userId, addToCartDto.ProductId, addToCartDto.Quantity);

                if (cartItem == null)
                {
                    _logger.LogWarning("Failed to add item to cart for user {UserId}", userId);
                    return BadRequest(new ApiResponse<CartItemDto>
                    {
                        Success = false,
                        Message = "Product not available or insufficient stock"
                    });
                }

                return Ok(new ApiResponse<CartItemDto>
                {
                    Success = true,
                    Data = cartItem,
                    Message = "Item added to cart successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");
                return StatusCode(500, new ApiResponse<CartItemDto>
                {
                    Success = false,
                    Message = "An error occurred while adding item to the cart"
                });
            }
        }

        /// <summary>
        /// Update cart item quantity
        /// </summary>
        [HttpPut("items/{cartItemId}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateCartItem(int cartItemId, [FromBody] UpdateCartItemDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid update data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var result = await _cartService.UpdateCartItemAsync(userId, cartItemId, updateDto.Quantity);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Cart item not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Cart item updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item {CartItemId}", cartItemId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while updating the cart item"
                });
            }
        }

        /// <summary>
        /// Remove item from cart
        /// </summary>
        [HttpDelete("items/{cartItemId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveFromCart(int cartItemId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var result = await _cartService.RemoveFromCartAsync(userId, cartItemId);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Cart item not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Item removed from cart successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart {CartItemId}", cartItemId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while removing the item from the cart"
                });
            }
        }

        /// <summary>
        /// Clear all items from cart
        /// </summary>
        [HttpDelete("clear")]
        public async Task<ActionResult<ApiResponse<bool>>> ClearCart()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                await _cartService.ClearCartAsync(userId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Cart cleared successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while clearing the cart"
                });
            }
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IEmailService _emailService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, IEmailService emailService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Get user's orders
        /// </summary> 
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<OrderDto>>>> GetUserOrders()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var orders = await _orderService.GetUserOrdersAsync(userId);

                return Ok(new ApiResponse<IEnumerable<OrderDto>>
                {
                    Success = true,
                    Data = orders,
                    Message = "Orders retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user orders");
                return StatusCode(500, new ApiResponse<IEnumerable<OrderDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving user orders"
                });
            }
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        [HttpGet("{Id}")]
        public async Task<ActionResult<ApiResponse<OrderDetailDto>>> GetOrderById(int Id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var order = await _orderService.GetOrderByIdAsync(Id, userId);

                if (order == null)
                {
                    return NotFound(new ApiResponse<OrderDetailDto>
                    {
                        Success = false,
                        Message = "Order not found"
                    });
                }

                return Ok(new ApiResponse<OrderDetailDto>
                {
                    Success = true,
                    Data = order,
                    Message = "Order retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", Id);
                return StatusCode(500, new ApiResponse<OrderDetailDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the order"
                });
            }
        }

        /// <summary>
        /// Create a new order from the cart
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<OrderDto>
                    {
                        Success = false,
                        Message = "Invalid order data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var order = await _orderService.CreateOrderAsync(userId, createOrderDto);

                if (order == null)
                {
                    return BadRequest(new ApiResponse<OrderDto>
                    {
                        Success = false,
                        Message = "Failed to create order"
                    });
                }

                // Send order confirmation email
                var userEmail = User.FindFirst("Email")?.Value;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendOrderConfirmationEmailAsync(userEmail, null);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send order confirmation email");
                        }
                    });
                }

                return CreatedAtAction(nameof(GetOrderById), new { Id = order.Id }, new ApiResponse<OrderDto>
                {
                    Success = true,
                    Data = order,
                    Message = "Order created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new ApiResponse<OrderDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the order"
                });
            }
        }

        /// <summary>
        /// Cancel an order
        /// </summary>
        [HttpDelete("{Id}/cancel")]
        public async Task<ActionResult<ApiResponse<bool>>> CancelOrder(int Id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var result = await _orderService.CancelOrderAsync(Id, userId);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Order not found or cannot be canceled"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Order cancelled successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling order {OrderId}", Id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while canceling the order"
                });
            }
        }

        /// <summary>
        /// Get order tracking information
        /// </summary>
        [HttpGet("{Id}/tracking")]
        public async Task<ActionResult<ApiResponse<TrackingInfoDto>>> GetTrackingInfo(int Id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var trackingInfo = await _orderService.GetTrackingInfoAsync(Id, userId);

                if (trackingInfo == null)
                {
                    return NotFound(new ApiResponse<TrackingInfoDto>
                    {
                        Success = false,
                        Message = "Tracking information not found"
                    });
                }

                return Ok(new ApiResponse<TrackingInfoDto>
                {
                    Success = true,
                    Data = trackingInfo,
                    Message = "Tracking information retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tracking information for order {OrderId}", Id);
                return StatusCode(500, new ApiResponse<TrackingInfoDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving tracking information"
                });
            }
        }
    }
}