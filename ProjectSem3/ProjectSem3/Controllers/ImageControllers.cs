using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ProjectSem3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageControllers : ControllerBase
    {
        [HttpGet("{imageName}")]
        public IActionResult GetImage(string imageName)
        {
            // Đường dẫn tới thư mục chứa ảnh
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", imageName);

            if (!System.IO.File.Exists(imagePath))
            {
                // Trả về lỗi 404 nếu ảnh không tồn tại
                return NotFound();
            }

            // Đọc dữ liệu ảnh và trả về dưới dạng file
            var imageBytes = System.IO.File.ReadAllBytes(imagePath);
            return File(imageBytes, "image/jpeg");
        }
    }
}
