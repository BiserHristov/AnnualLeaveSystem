﻿namespace AnnualLeaveSystem.Controllers
{
    using AnnualLeaveSystem.Infrastructure;
    using AnnualLeaveSystem.Services.Leaves;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    public class StatisticController : Controller
    {
        private readonly ILeaveService leaveService;

        public StatisticController(ILeaveService leaveService)
        {
            this.leaveService = leaveService;
        }

        [Authorize]
        public IActionResult History()
        {
            var leaves = this.leaveService.ByEmployee(this.User.GetId());

            return View(leaves);
        }
    }
}
