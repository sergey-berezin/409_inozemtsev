using Database;
using Microsoft.AspNetCore.Mvc;

namespace ServerClasses.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class imagesController : ControllerBase
    {
        private ControllerFunctions Functions;
        private readonly ILogger<imagesController> _logger;

        public imagesController(ILogger<imagesController> logger)
        {
            this._logger = logger;
            this.Functions = new ControllerFunctions();
        }

        [HttpGet]
        public async Task<ActionResult<int[]>> GetAll(CancellationToken token)
        {
            (bool res, int[] IDs) = await Functions.GetAllImages(token);
            if (res)
                if (IDs == null)
                    return StatusCode(404, $"No images in the current database.");
                else
                    return IDs;
            else
                return StatusCode(400, "The request has been canceled.");

        }

        [HttpGet("id")]
        public async Task<ActionResult<Database.Image>> Get(int id, CancellationToken token)
        {
            (bool res, Database.Image? image) = await Functions.TryGetImageByID(id, token);
            if (res)
                if (image == null)
                    return StatusCode(404, $"Image with ID {id} doesn't exist in the current database.");
                else
                    return image;
            else
                return StatusCode(400, "The request has been canceled.");
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post([FromBody]PostData data, CancellationToken token)
        {
            (bool res, int id) = await Functions.PostImage(data, token);
            if (res)
                if (id == -1)
                    return StatusCode(500, "An exception has occurred.");
                else
                    return id;
            else
                return StatusCode(400, "The request has been canceled.");
        }

        [HttpDelete]
        public async Task<ActionResult<int>> Delete(CancellationToken token)
        {
            int res = await Functions.DeleteAllImages(token);
            if (res == -1)
                return StatusCode(500, "An exception has occurred.");
            else if (res == 0)
                return StatusCode(400, "The request has been canceled.");
            else
                return res;
        }
    }
}
