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
                var result = new object();
                using (var client = new HttpClient())
                {
                    Uri endpointToken = new Uri("http://192.168.20.2:8181/rcms/services/rest/hikRpcService/genAgvSchedulingTask/");

                    var paramObj = new
                    {
                        reqCode = request.ReqCode,
                        taskType = request.TaskTyp,
                        PositionCodePath = request.PositionCodePath,
                    };
                    var dataJson = JsonConvert.SerializeObject(paramObj);
                    var payload = new StringContent(dataJson, Encoding.UTF8, "application/json");


                    var jsonresult = client.PostAsync(endpointToken, payload).Result.Content.ReadAsStringAsync().Result;
                    result = JsonConvert.DeserializeObject<Response>(jsonresult); //This is the result from SSO


                }
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
