using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/items")]
    public class ItemController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly IUserRecentlyViewedService _recentlyViewedService;

        public ItemController(IItemService itemService, IUserRecentlyViewedService recentlyViewedService)
        {
            _itemService = itemService;
            _recentlyViewedService = recentlyViewedService;
        }

        //GET every approved and active items in db
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var items = await _itemService.GetAllApprovedAsync();
            return Ok(items);
        }

        //GET every item in db regardless of status - ADMIN only 
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAdmin([FromQuery] bool includeInactive = false)
        {
            var items = await _itemService.GetAllForAdminAsync(includeInactive);
            return Ok(items);
        }

        //GET item by id (public, but owners see their own pending/rejected too)
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin"); //Admin can get all items by id regardless of status 
            var item = await _itemService.GetByIdAsync(id, userId, isAdmin);

            // Silently track view for authenticated non-admin users — fire and forget
            if (userId != null && !isAdmin)
                await _recentlyViewedService.TrackViewAsync(userId, id);

            return Ok(item);
        }

        //GET all loggedin user's items (every item ofc)
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyItems()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var items = await _itemService.GetByOwnerAsync(userId);
            return Ok(items);
        }

        //GET - admin gets all items by a specific user - all status
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByUser(string userId)
        {
            var items = await _itemService.GetByOwnerAsync(userId);
            return Ok(items);
        }

        //All approved items by a user 
        [HttpGet("user/{userId}/public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByUserPublic(string userId)
        {
            var items = await _itemService.GetPublicByOwnerAsync(userId);
            return Ok(items);
        }

        //GET f.x. api/items/nearby?lat=55.6&lng=12.5&radiusKm=10
        [HttpGet("nearby")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNearby([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radiusKm = 10)
        {
            var items = await _itemService.GetNearbyAsync(lat, lng, radiusKm);
            return Ok(items);
        }




        //POST Create item
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] ItemDTO.CreateItemDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            var item = await _itemService.CreateAsync(userId, dto, isAdmin);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }

        //update item (owner only) and admin
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] ItemDTO.UpdateItemDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            var item = await _itemService.UpdateAsync(id, userId, dto, isAdmin);
            return Ok(item);
        }

        //update — admin changes item status
        [HttpPatch("admin/{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] ItemDTO.AdminItemStatusDTO dto)
        {
            var item = await _itemService.UpdateStatusAsync(id, dto);
            return Ok(item);
        }

        //DELETE item (owner only) and admin
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            await _itemService.DeleteAsync(id, userId, isAdmin);
            return NoContent();
        }

        //GET QR code for an item (owner only) and admin
        [HttpGet("{id}/qrcode")]
        [Authorize]
        public async Task<IActionResult> GetQrCode(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            var qr = await _itemService.GetQrCodeAsync(id, userId, isAdmin);
            return Ok(qr);
        }


        //Scan qr code by borrower to confirm pickup or return
        [HttpPost("scan")]
        [Authorize]
        public async Task<IActionResult> Scan([FromQuery] string qrCode)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            var item = await _itemService.ScanQrCodeAsync(qrCode, userId, isAdmin);
            return Ok(item);
        }

        //ADmin approval queue
        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var items = await _itemService.GetPendingApprovalsAsync();
            return Ok(items);
        }

        //Admin decide (approve or reject)
        [HttpPost("admin/{id}/decide")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDecide(int id, [FromBody] ItemDTO.AdminItemDecisionDTO dto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var item = await _itemService.AdminDecideAsync(id, adminId, dto);
            return Ok(item);
        }





    }
}
