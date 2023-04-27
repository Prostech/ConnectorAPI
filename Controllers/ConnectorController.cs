using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
using System.Text.RegularExpressions;

namespace RozitekAPIConnector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectorController : ControllerBase
    {
        private readonly AppSettings _appConfig;
        private readonly ILogger<ConnectorController> _logger;

        public ConnectorController(IOptions<AppSettings> appConfig, ILogger<ConnectorController> logger)
        {
            _appConfig = appConfig.Value;
            _logger = logger;
        }

        [HttpPost("return-pod-factory-2-1")]
        public async Task<IActionResult> ReturnPodFactory2to1Async([FromBody] ReturnPodFactory2to1Request req)
        {
            DateTime utcTime = DateTime.UtcNow;
            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, cstZone);
            try
             {
                //Get PodCode And Mat
                PodAndPositionResult getPodAndMatRes = await FindPodAtPosition(req.Position);

                if (string.IsNullOrEmpty(getPodAndMatRes.PodCode))
                {
                    var returnMessage = new
                    {
                        Id = -1,
                        Message = $"There is no pod at position: {req.Position}",
                    };
                    _logger.LogError($"{returnMessage.ToString()} || {cstTime}");
                    return new BadRequestObjectResult(returnMessage);
                }    

                if (!string.IsNullOrEmpty(getPodAndMatRes.CaseNum))
                {
                    //Unbind Mat and Pod
                    ReturnMessage unBindCmdRes = await UnBindPodAndMat(getPodAndMatRes.CaseNum, getPodAndMatRes.PodCode);

                    if (!unBindCmdRes.Code.Equals("0", StringComparison.OrdinalIgnoreCase))
                    {
                        var returnMessage = new
                        {
                            Id = -1,
                            Message = "Unbind failed",
                            ErrorMessage = unBindCmdRes.Message,
                        };
                        _logger.LogError($"{returnMessage.ToString()} || {cstTime}");
                        return new BadRequestObjectResult(returnMessage);
                    }    
                }

                string binCode = getPodAndMatRes.PodCode + _appConfig.BinCodeSuffix;

                //getOutPod count
                var countRes = await CountTaskByStatusAsync(req.Position, _appConfig.CountTaskRequest.TaskTyp);
                if (countRes > 0)
                {
                    ReturnMessage returnPodCmdRes = await returnPod(getPodAndMatRes.PodCode, binCode, req.Position);
                    if (!returnPodCmdRes.Code.Equals("0", StringComparison.OrdinalIgnoreCase))
                    {
                        var returnMessage = new
                        {
                            Id = -1,
                            Message = "returnPod failed",
                            ErrorMessage = returnPodCmdRes.Message,
                        };
                        _logger.LogError($"{returnMessage.ToString()} || {cstTime}");
                        return new BadRequestObjectResult(returnMessage);
                    }
                }
                else
                {
                    ReturnMessage returnPodCmdRes = await returnPod(getPodAndMatRes.PodCode, binCode, req.Position);
                    if (!returnPodCmdRes.Code.Equals("0", StringComparison.OrdinalIgnoreCase))
                    {
                        var returnMessage = new
                        {
                            Id = -1,
                            Message = "returnPod failed",
                            ErrorMessage = returnPodCmdRes.Message,
                        };
                        _logger.LogError($"{returnMessage.ToString()} || {cstTime}");
                        return new BadRequestObjectResult(returnMessage);
                    }    

                    ReturnMessage getOutPodCmdRes = await getOutPod(getPodAndMatRes.PodCode, binCode, req.Position);
                    if (!getOutPodCmdRes.Code.Equals("0", StringComparison.OrdinalIgnoreCase))
                    {
                        var returnMessage = new
                        {
                            Id = -1,
                            Message = "getOutPod failed",
                            ErrorMessage = getOutPodCmdRes.Message,
                        };
                        _logger.LogError($"{returnMessage.ToString()} || {cstTime}");
                        return new BadRequestObjectResult(returnMessage);
                    }
                }

                _logger.LogInformation($"Run API Success || {cstTime}");
                return new JsonResult(new
                {
                    Id = 1,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                var returnMessage = new
                {
                    Id = -1,
                    Message = ex.Message
                };
                _logger.LogError($"{returnMessage.ToString()} || {cstTime}");
                return new BadRequestObjectResult(returnMessage);
            }
        }

        [HttpPost("return-mat-factory-1-2")]
        public async Task<IActionResult> ReturnMatFactory1to2([FromBody] ReturnMatFactory1to2Request req, int offset)
        {
            DateTime utcTime = DateTime.UtcNow;
            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, cstZone);
            try
            {
                PodAndPositionResult findPodRes = new PodAndPositionResult();
                findPodRes = await FindPodAtPosition(req.Position);

                int countTaskBringPodToPosition = new int();
                countTaskBringPodToPosition = await CountTaskByStatusAsync(req.Position, _appConfig.CountTaskRequest.TaskTyp);

                _logger.LogInformation($"getOutPod executing quantity: {countTaskBringPodToPosition} || {cstTime}");

                if (string.IsNullOrEmpty(findPodRes.PodCode) && countTaskBringPodToPosition == 0)
                {
                    string podCode = new string("");
                    podCode = await FindFreePodByArea(_appConfig.GetOutPodParams.Area, offset);
                    if (string.IsNullOrEmpty(podCode))
                    {
                        var returnMessage = new
                        {
                            Id = -1,
                            Message = $"There is no free pod at area: {_appConfig.GetOutPodParams.Area}",
                        };
                        _logger.LogError($"{returnMessage.ToString()} || {cstTime}");
                        return new BadRequestObjectResult(returnMessage);
                    }
                    _logger.LogInformation($"Pod free at area {_appConfig.GetOutPodParams.Area}: {podCode} || {cstTime}");

                    string binCode = podCode + _appConfig.BinCodeSuffix;
                    //getOutPod
                    ReturnMessage getOutPodRes = new ReturnMessage();
                    getOutPodRes = await getOutPod(podCode, binCode, req.Position);

                    if (!getOutPodRes.Code.Equals("0", StringComparison.OrdinalIgnoreCase))
                    {
                        var returnMessage = new
                        {
                            Id = -1,
                            Message = "getOutPod failed",
                            ErrorMessage = getOutPodRes.Message,
                        };
                        _logger.LogError($"{returnMessage.ToString()} || {cstTime}");
                        return new BadRequestObjectResult(returnMessage);
                    }

                    _logger.LogInformation($"Run API Success || {cstTime}");
                    return new JsonResult(new
                    {
                        Id = 1,
                        Message = "Success"
                    });
                }
                else
                {
                    var returnMessage = new
                    {
                        Id = -1,
                        Message = "There is Pod " + findPodRes.PodCode + " at " + findPodRes.Place,
                    };
                    _logger.LogInformation($"{returnMessage.ToString()} || {cstTime}");
                    return new BadRequestObjectResult(returnMessage);
                }
            }
            catch (Exception ex)
            {
                var returnMessage = new
                {
                    Id = -1,
                    Message = ex.Message
                };
                _logger.LogError($"{returnMessage.ToString()} || {cstTime}");
                return new BadRequestObjectResult(returnMessage);
            }
        }

        private async Task<PodAndPositionResult> FindPodAtPosition(string Position)
        {
            try
            {
                string query = @"SELECT tmd.map_data_code, tmd.pod_code, tp.case_num
                                FROM tcs_map_data tmd
                                left join tcs_pod tp
                                on tp.pod_code = tmd. pod_code
                                where tmd.map_data_code = @p_position
                                limit 1;";

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
                    pod.PodCode = row["pod_code"].ToString();
                    pod.Place = row["map_data_code"].ToString();
                    pod.CaseNum = row["case_num"].ToString();
                    result.PodCode = pod.PodCode;
                    result.Place = pod.Place;
                    result.CaseNum = pod.CaseNum;
                }

                return result;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private async Task<string> FindFreePodByArea(string area, int offset)
        {
            try
            {
                string query = @"SELECT tmd.pod_code
                                FROM tcs_map_data tmd
                                LEFT JOIN tcs_pod tp 
                                    ON tp.pod_code = tmd.pod_code 
                                WHERE tmd.area_code = @p_area
                                AND (tp.case_num IS NULL OR tp.case_num = '')
                                and tmd.pod_code is not null
                                and tmd.pod_code <> ''
                                ORDER BY coo_x asc, tmd.pod_code
								limit 1
                                offset @p_offset;";

                DataTable table = new DataTable();
                string sqlDataSource = _appConfig.DbConnection;
                NpgsqlDataReader myReader;
                using (NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource))
                {
                    myCon.Open();
                    using (NpgsqlCommand myCommand = new NpgsqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@p_area", area);
                        myCommand.Parameters.AddWithValue("@p_offset", offset);

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
                    pod = row["pod_code"].ToString();
                    result = pod;
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
                    Uri endpoint = new Uri($"{_appConfig.RCSUrl}/rcms/services/rest/hikRpcService/bindPodAndMat"); // API endpoint URL

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

        private async Task<ReturnMessage> returnPod(string Pod, string Bin, string Position)
        {
            try
            {
                ReturnMessage result = new ReturnMessage();

                using (var client = new HttpClient()) // Create an HTTP client to make API call
                {
                    Uri endpoint = new Uri($"{_appConfig.RCSUrl}/rcms/services/rest/hikTpsService/returnPod"); // API endpoint URL

                    var paramObj = new // Create an anonymous object to hold request parameters
                    {
                        reqCode = GenerateRandomString(32), // Request code
                        binCode = Bin,
                        podCode = Pod,
                        wbCode = Position,
                        taskTyp = _appConfig.ReturnPodParams.TaskTyp,
                        returnPodStrategy = _appConfig.ReturnPodParams.ReturnPodStrategy,
                        taskCode = GenerateRandomString(32),
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

        private async Task<ReturnMessage> getOutPod(string Pod, string Bin, string Position)
        {
            try
            {
                ReturnMessage result = new ReturnMessage();

                using (var client = new HttpClient()) // Create an HTTP client to make API call
                {
                    Uri endpoint = new Uri($"{_appConfig.RCSUrl}/rcms/services/rest/hikTpsService/getOutPod"); // API endpoint URL

                    var paramObj = new // Create an anonymous object to hold request parameters
                    {
                        reqCode = GenerateRandomString(32), // Request code
                        taskTyp = _appConfig.GetOutPodParams.TaskTyp,
                        data = new[]
                        { new
                        {
                            taskCode = GenerateRandomString(32),
                            binCode = Bin,
                            wbCode = Position,
                            podCode = Pod,
                            groupId = _appConfig.GetOutPodParams.GroupId,
                            liftStatus = _appConfig.GetOutPodParams.LiftStatus,
                            pickTime = _appConfig.GetOutPodParams.PickTime,
                            priority = _appConfig.GetOutPodParams.Priority
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

        private async Task<int> CountTaskByStatusAsync(string Position, string taskTyp)
        {
            try
            {
                // SQL query to call stored function
                string query = @"SELECT count(*)
                                FROM tcs_trans_task
                                WHERE task_status = @p_task_status
                                    AND (@p_task_typ = '' OR task_typ = @p_task_typ)
                                    AND wb_code = @p_wb_code;";

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
                        myCommand.Parameters.AddWithValue("@p_task_status", _appConfig.CountTaskRequest.TaskStatus);

                        // Add parameter for task type
                        myCommand.Parameters.AddWithValue("@p_task_typ", taskTyp);

                        // Add parameter for wb codes
                        myCommand.Parameters.AddWithValue("@p_wb_code", Position);

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

        private async Task<int> FindExecutingTaskToPosition(string Position)
        {
            try
            {
                // SQL query to call stored function
                string query = @"SELECT count(*)
                                    FROM tcs_trans_task
                                    WHERE task_status = @p_task_status
                                        AND task_typ = @p_task_typ
                                        AND wb_code = @p_wb_code;";

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
                        myCommand.Parameters.AddWithValue("@p_task_status", _appConfig.CountTaskRequest.TaskStatus);

                        // Add parameter for task type
                        // Add parameter for wb codes
                        myCommand.Parameters.AddWithValue("@p_wb_code", Position);

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
                result = _appConfig.DbConnection;
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
