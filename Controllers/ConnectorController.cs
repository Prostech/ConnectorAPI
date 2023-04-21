using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Npgsql;
using RozitekAPIConnector.Models;
using System;
using System.Data;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json.Nodes;
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

        [HttpPost("return-pod-factory-2-1")]
        public async Task<IActionResult> ReturnPodFactory2to1Async([FromBody] ReturnPodFactory2to1Request req)
        {
            try
            {
                //Get PodCode And Mat
                TCSPodResult getPodAndMatRes = await QueryPodCodeAndMatAsync(req.Position);

                
                if (getPodAndMatRes.CaseNum != null && getPodAndMatRes.CaseNum != "")
                {
                    //Unbind Mat and Pod
                    ReturnMessage unBindCmdRes = await UnBindPodAndMat(getPodAndMatRes.CaseNum, getPodAndMatRes.PodCode);

                    if (!unBindCmdRes.Code.Equals("0", StringComparison.OrdinalIgnoreCase))
                        return new BadRequestObjectResult(new
                        {
                            Id = -1,
                            Message = "Unbind failed",
                            ErrorMessage = unBindCmdRes.Message,
                        });
                }

                string binCode = getPodAndMatRes.PodCode + _appConfig.BinCodeSuffix;

                //getOutPod count
                var countRes = await CountTaskByStatusAsync(req.countTaskRequest.TaskStatus, req.countTaskRequest.TaskTyp, req.countTaskRequest.WbCodes);
                if (countRes > 0)
                {
                    ReturnMessage returnPodCmdRes = await returnPod(getPodAndMatRes.CaseNum, getPodAndMatRes.PodCode, binCode, req.ReturnPodStrategy, req.Position, req.TaskTyp);
                    if (!returnPodCmdRes.Code.Equals("0", StringComparison.OrdinalIgnoreCase))
                        return new BadRequestObjectResult(new
                        {
                            Id = -1,
                            Message = "returnPod failed",
                            ErrorMessage = returnPodCmdRes.Message,
                        });
                }
                else
                {
                    return new JsonResult(new
                        {
                            Id = 1,
                            Message = "There is not getOutPod API executing",
                        });
                }

                return new JsonResult(new
                {
                    Id = 1,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new
                {
                    Id = -1,
                    Message= ex.Message
                });
            }
        }

        [HttpPost("return-mat-factory-1-2")]
        public async Task<IActionResult> ReturnMatFactory1to2([FromBody] ReturnMatFactory1to2Request req)
        {
            try
            {
                PodAndPositionResult findPodRes = new PodAndPositionResult();
                findPodRes = await FindPodAtPosition(req.Position);
                if (findPodRes.PodCode == null || findPodRes.PodCode == "") 
                {
                    string podCode = new string("");
                    podCode = await FindFreePodByArea(req.Area);

                    //getOutPod
                    ReturnMessage getOutPodRes = new ReturnMessage();
                    getOutPodRes = await getOutPod("", podCode, podCode + "56501013", "", req.Position, req.TaskTyp);
                    if (!getOutPodRes.Code.Equals("0", StringComparison.OrdinalIgnoreCase))
                        return new BadRequestObjectResult(new
                        {
                            Id = -1,
                            Message = "getOutPod failed",
                            ErrorMessage = getOutPodRes.Message,
                        });
                }
                else
                {
                    return new JsonResult(new
                    {
                        Id = 1,
                        Message = "There is Pod " +findPodRes.PodCode+" at "+findPodRes.Place,
                    });
                }

                return new JsonResult(new
                {
                    Id = 1,
                    Message = "Success",
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new
                {
                    Id = -1,
                    Message = ex.Message
                });
            }
        }

        private async Task<PodAndPositionResult> FindPodAtPosition(string Position)
        {
            try
            {
                string query = @"select * from find_pod_by_position(@p_position)";

                DataTable table = new DataTable();
                string sqlDataSource = _appConfig.DbConnection;
                NpgsqlDataReader myReader;
                using (NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource))
                {
                    myCon.Open();
                    using (NpgsqlCommand myCommand = new NpgsqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@p_position", Position);

                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);

                        myReader.Close();
                        myCon.Close();

                    }
                }

                // Convert DataTable to List<TCSPodResult>
                PodAndPositionResult result = new PodAndPositionResult();
                foreach (DataRow row in table.Rows)
                {
                    PodAndPositionResult pod = new PodAndPositionResult();
                    pod.PodCode = row["PodCode"].ToString();
                    pod.Place = row["Place"].ToString();
                    result.PodCode = pod.PodCode;
                    result.Place = pod.Place;
                }

                return result;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private async Task<string> FindFreePodByArea(string area)
        {
            try
            {
                string query = @"select * from find_free_pod_by_area(@p_area)";

                DataTable table = new DataTable();
                string sqlDataSource = _appConfig.DbConnection;
                NpgsqlDataReader myReader;
                using (NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource))
                {
                    myCon.Open();
                    using (NpgsqlCommand myCommand = new NpgsqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@p_area", area);

                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);

                        myReader.Close();
                        myCon.Close();

                    }
                }

                // Convert DataTable to List<TCSPodResult>
                string result = new string("");
                foreach (DataRow row in table.Rows)
                {
                    string pod = new string("string");
                    pod = row["PodCode"].ToString();
                    result = pod;
                }

                return result;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private async Task<TCSPodResult> QueryPodCodeAndMatAsync (string Position)
        {
            try
            {
                string query = @"select * from get_tcs_pod(@p_position, @p_task_status)";

                DataTable table = new DataTable();
                string sqlDataSource = _appConfig.DbConnection;
                NpgsqlDataReader myReader;
                using (NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource))
                {
                    myCon.Open();
                    using (NpgsqlCommand myCommand = new NpgsqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@p_position", Position);
                        myCommand.Parameters.AddWithValue("@p_task_status", "9");

                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);

                        myReader.Close();
                        myCon.Close();

                    }
                }

                // Convert DataTable to List<TCSPodResult>
                TCSPodResult result = new TCSPodResult();
                foreach (DataRow row in table.Rows)
                {
                    TCSPodResult pod = new TCSPodResult();
                    pod.PodCode = row["PodCode"].ToString();
                    pod.CaseNum = row["CaseNum"].ToString();
                    result.PodCode = pod.PodCode;
                    result.CaseNum = pod.CaseNum;
                }

                return result;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private async Task<ReturnMessage> UnBindPodAndMat(string Mat, string Pod)
        {
            try
            {
                ReturnMessage result = new ReturnMessage();

                using (var client = new HttpClient()) // Create an HTTP client to make API call
                {
                    Uri endpoint = new Uri(_appConfig.Url + "/rcms/services/rest/hikRpcService/bindPodAndMat"); // API endpoint URL

                    var paramObj = new // Create an anonymous object to hold request parameters
                    {
                        reqCode = GenerateRandomString(32), // Request code
                        podCode = Pod, // Area code
                        materialLot = Mat, // Position code
                        indBind = "0"
                    };

                    var dataJson = JsonConvert.SerializeObject(paramObj); // Serialize request parameters to JSON
                    var payload = new StringContent(dataJson, Encoding.UTF8, "application/json"); // Create a StringContent object with serialized JSON as payload

                    var resultString = client.PostAsync(endpoint, payload).Result.Content.ReadAsStringAsync().Result; // Send POST request to API, read response content as string and store in 'result' variable
                    result = JsonConvert.DeserializeObject<ReturnMessage>(resultString);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<ReturnMessage> returnPod(string Mat, string Pod, string Bin, string Strategy, string Position, string TaskTyp)
        {
            try
            {
                ReturnMessage result = new ReturnMessage();

                using (var client = new HttpClient()) // Create an HTTP client to make API call
                {
                    Uri endpoint = new Uri(_appConfig.Url + "/rcms/services/rest/hikTpsService/returnPod"); // API endpoint URL

                    var paramObj = new // Create an anonymous object to hold request parameters
                    {
                        reqCode = GenerateRandomString(16), // Request code
                        binCode = Bin,
                        podCode = Pod,
                        wbCode = Position,
                        taskTyp = TaskTyp,
                        returnPodStrategy = Strategy,
                        taskCode = GenerateRandomString(16),
                    };
                    var dataJson = JsonConvert.SerializeObject(paramObj); // Serialize request parameters to JSON
                    var payload = new StringContent(dataJson, Encoding.UTF8, "application/json"); // Create a StringContent object with serialized JSON as payload

                    var resultString = client.PostAsync(endpoint, payload).Result.Content.ReadAsStringAsync().Result; // Send POST request to API, read response content as string and store in 'result' variable
                    result = JsonConvert.DeserializeObject<ReturnMessage>(resultString);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<ReturnMessage> getOutPod(string Mat, string Pod, string Bin, string Strategy, string Position, string TaskTyp)
        {
            try
            {
                ReturnMessage result = new ReturnMessage();

                using (var client = new HttpClient()) // Create an HTTP client to make API call
                {
                    Uri endpoint = new Uri(_appConfig.Url + "/rcms/services/rest/hikTpsService/getOutPod"); // API endpoint URL

                    var paramObj = new // Create an anonymous object to hold request parameters
                    {
                        reqCode = GenerateRandomString(16), // Request code
                        taskTyp = TaskTyp,
                        data = new[]
                        { new
                        {
                            taskCode = GenerateRandomString(16),
                            binCode = Bin,
                            wbCode = Position,
                            podCode = Pod,
                        } }
                    };
                    var dataJson = JsonConvert.SerializeObject(paramObj); // Serialize request parameters to JSON
                    var payload = new StringContent(dataJson, Encoding.UTF8, "application/json"); // Create a StringContent object with serialized JSON as payload

                    var resultString = client.PostAsync(endpoint, payload).Result.Content.ReadAsStringAsync().Result; // Send POST request to API, read response content as string and store in 'result' variable
                    result = JsonConvert.DeserializeObject<ReturnMessage>(resultString);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<int> CountTaskByStatusAsync(string TaskStatus, string TaskTyp, string[] WbCodes)
        {
            try
            {
                // SQL query to call stored function
                string query = @"select * from count_tcs_task_by_status(@p_task_status, @p_task_typ, @p_wb_codes)";

                // variable to hold the value of the quantity property
                int quantity = 0;

                // Connection string for database
                string sqlDataSource = _appConfig.DbConnection;

                // Create and open database connection
                using (NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource))
                {
                    await myCon.OpenAsync();

                    // Create and execute database command
                    using (NpgsqlCommand myCommand = new NpgsqlCommand(query, myCon))
                    {
                        // Add parameter for task status
                        myCommand.Parameters.AddWithValue("@p_task_status", TaskStatus);

                        // Add parameter for task type
                        myCommand.Parameters.AddWithValue("@p_task_typ", TaskTyp);

                        // Add parameter for wb codes
                        myCommand.Parameters.AddWithValue("@p_wb_codes", WbCodes);

                        // Execute query and read result into data reader
                        NpgsqlDataReader myReader = await myCommand.ExecuteReaderAsync();

                        // Check if there is a row in the result set
                        if (myReader.Read())
                        {
                            // Extract the value of the quantity property from the first column
                            quantity = myReader.GetInt32(0);
                        }

                        // Close data reader
                        myReader.Close();

                        // Close database connection
                        myCon.Close();
                    }
                }

                // Return quantity as result
                return quantity;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private string GenerateRandomString(int limit)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            string randomString = new string(Enumerable.Repeat(chars, limit)
              .Select(s => s[random.Next(s.Length)])
              .ToArray());
            return randomString;
        }

        [HttpGet("test-deploy")]
        public async Task<IActionResult> TestMiddleware()
        {
            try
            {
                string result = new string("");
                result = "Deploy v1.4";
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
