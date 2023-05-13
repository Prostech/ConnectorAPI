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
    public class DisableTask : IJob
    {
        private readonly ILogger<DisableTask> _logger;
        private readonly AppSettings _appConfig;
        private readonly ConnectorController _controller;

        public DisableTask(ILogger<DisableTask> logger, IOptions<AppSettings> appConfig, ConnectorController connector)
        {
            _logger = logger;
            _appConfig = appConfig.Value;
            _controller = connector;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                string[] positions = _appConfig.CancelTaskParams.Positions.Split(',');

                foreach (string position in positions)
                {
                    CancelTaskReq req = new CancelTaskReq();
                    req.Position = position;
                    await _controller.CancelTaskAsync(req);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message.ToString());
                throw;
            }
        }

    }
}
