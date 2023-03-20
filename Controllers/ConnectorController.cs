using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RozitekAPIConnector.Models;
using System.Text;
using System.Text.Json.Serialization;

namespace RozitekAPIConnector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectorController : ControllerBase
    {
        private readonly AppSettings _appConfig;

        public ConnectorController(IOptions<AppSettings> appConfig)
        {
            _appConfig = appConfig.Value;
        }
        [HttpPost]
        public async Task<IActionResult> Middleware([FromBody] Request request)
        {
            try
            {
                string result = new string("");
                using (var client = new HttpClient())
                {
                    Uri endpointToken = new Uri(_appConfig.Url);

                    var paramObj = new
                    {
                        reqCode = request.ReqCode,
                        taskType = request.TaskTyp,
                        PositionCodePath = request.PositionCodePath,
                    };
                    var dataJson = JsonConvert.SerializeObject(paramObj);
                    var payload = new StringContent(dataJson, Encoding.UTF8, "application/json");


                    result = client.PostAsync(endpointToken, payload).Result.Content.ReadAsStringAsync().Result;
                }
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [HttpGet("test-deploy")]
        public async Task<IActionResult> TestMiddleware()
        {
            try
            {
                string result = new string("");
                result = "Deploy OK";
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
