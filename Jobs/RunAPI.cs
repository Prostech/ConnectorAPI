using Newtonsoft.Json;
using Quartz;
using RozitekAPIConnector.Models;
using System.Text;
using System;
using static System.Net.WebRequestMethods;
using Microsoft.Extensions.Options;
using RozitekAPIConnector.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace RozitekAPIConnector.Jobs
{
    [DisallowConcurrentExecution]
    public class RunAPI : IJob
    {
        private readonly ILogger<RunAPI> _logger;
        private readonly AppSettings _appConfig;
        private readonly ConnectorController _controller;

        public RunAPI(ILogger<RunAPI> logger, IOptions<AppSettings> appConfig, ConnectorController connector)
        {
            _logger = logger;
            _appConfig = appConfig.Value;
            _controller = connector;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            DateTime utcTime = DateTime.UtcNow;
            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, cstZone);
            try
            {
                string[] positions = _appConfig.Positions.Split(',');

                foreach (string position in positions)
                {
                    ReturnMatFactory1to2Request req = new ReturnMatFactory1to2Request();
                    req.Position = position;
                    await _controller.ReturnMatFactory1to2(req);
                    _logger.LogInformation($"Job done: {position} || {cstTime}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message.ToString() +cstZone);
                throw;
            }
            return;
        }

    }
}
