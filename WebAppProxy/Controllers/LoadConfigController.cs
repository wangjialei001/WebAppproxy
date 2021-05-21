using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppProxy.Services;

namespace WebAppProxy.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LoadConfigController: ControllerBase
    {
        private readonly ILoadConfigService _loadConfigService;
        public LoadConfigController(ILoadConfigService loadConfigService)
        {
            _loadConfigService = loadConfigService;
        }
        [HttpGet]
        public async Task LoadConfig()
        {
            await _loadConfigService.GetChange();
        }
    }
}
