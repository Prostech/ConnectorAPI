using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Npgsql;
using RozitekAPIConnector.Models;
using System.Data;
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

        [HttpPost("queryPodBerthAndMat")]
        public async Task<IActionResult> QueryPodBerthAndMat([FromBody] QueryPodBerthAndMat request, [FromHeader] string token)
        {
            try
            {
                string result = new string(""); // Variable to hold the result of the API call

                using (var client = new HttpClient()) // Create an HTTP client to make API call
                {
                    Uri endpointToken = new Uri(_appConfig.Url + "rcms/services/rest/hikRpcService/queryPodBerthAndMat"); // API endpoint URL

                    var paramObj = new // Create an anonymous object to hold request parameters
                    {
                        reqCode = request.ReqCode, // Request code
                        areaCode = request.AreaCode, // Area code
                        positionCode = request.PositionCode // Position code
                    };
                    var dataJson = JsonConvert.SerializeObject(paramObj); // Serialize request parameters to JSON
                    var payload = new StringContent(dataJson, Encoding.UTF8, "application/json"); // Create a StringContent object with serialized JSON as payload

                    result = client.PostAsync(endpointToken, payload).Result.Content.ReadAsStringAsync().Result; // Send POST request to API, read response content as string and store in 'result' variable
                }

                return new JsonResult(result); // Return 'result' as JSON response
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message); // Return error message as BadRequest response
            }
        }


        [HttpGet("task-order")]
        public async Task<IActionResult> QueryTaskOrderAsync([FromQuery] PaginatorModel page)
        {
            try
            {
                string query = @"select * from get_tcs_trans_task(@p_page_size, @p_page_number)"; // SQL query to call stored function

                DataTable table = new DataTable(); // DataTable to hold query result

                string sqlDataSource = _appConfig.DbConnection; // Connection string for database
                NpgsqlDataReader myReader; // Data reader to read query result
                using (NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource)) // Create and open database connection
                {
                    myCon.Open();

                    using (NpgsqlCommand myCommand = new NpgsqlCommand(query, myCon)) // Create and execute database command
                    {
                        myCommand.Parameters.AddWithValue("@p_page_size", page.PageSize); // Add parameter for page size
                        myCommand.Parameters.AddWithValue("@p_page_number", page.PageNumber); // Add parameter for page number

                        myReader = myCommand.ExecuteReader(); // Execute query and read result into data reader
                        table.Load(myReader); // Load data reader into data table

                        myReader.Close(); // Close data reader
                        myCon.Close(); // Close database connection
                    }
                }

                return new JsonResult(table); // Return data table as JSON result
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message); // Return error message as BadRequest response
            }
        }

        [HttpGet("count-task-by-status")]
        public async Task<IActionResult> CountTaskByStatusAsync([FromQuery] string TaskStatus, [FromQuery] string TaskTyp, [FromQuery] string[] WbCodes)
        {
            try
            {
                string query = @"select * from count_tcs_task_by_status(@p_task_status, @p_task_typ, @p_wb_codes)"; // SQL query to call stored function

                DataTable table = new DataTable(); // DataTable to hold query result

                string sqlDataSource = _appConfig.DbConnection; // Connection string for database
                NpgsqlDataReader myReader; // Data reader to read query result

                using (NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource)) // Create and open database connection
                {
                    myCon.Open();

                    using (NpgsqlCommand myCommand = new NpgsqlCommand(query, myCon)) // Create and execute database command
                    {
                        myCommand.Parameters.AddWithValue("@p_task_status", TaskStatus); // Add parameter for task status
                        myCommand.Parameters.AddWithValue("@p_task_typ", TaskTyp); // Add parameter for task type
                        myCommand.Parameters.AddWithValue("@p_wb_codes", WbCodes); // Add parameter for wb codes

                        myReader = myCommand.ExecuteReader(); // Execute query and read result into data reader
                        table.Load(myReader); // Load data reader into data table

                        myReader.Close(); // Close data reader
                        myCon.Close(); // Close database connection
                    }
                }

                return new JsonResult(table); // Return data table as JSON result
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message); // Return error message as BadRequest response
            }
        }


        [HttpGet("test-deploy")]
        public async Task<IActionResult> TestMiddleware()
        {
            try
            {
                string result = new string("");
                result = "Deploy v1.3";
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
