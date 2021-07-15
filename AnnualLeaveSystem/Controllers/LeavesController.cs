﻿namespace AnnualLeaveSystem.Controllers
{
    using AnnualLeaveSystem.Data;
    using AnnualLeaveSystem.Data.Models;
    using AnnualLeaveSystem.Models.Leaves;
    using AnnualLeaveSystem.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using static AnnualLeaveSystem.Data.DataConstants;
    public class LeavesController : Controller
    {
        private readonly IGetLeaveTypesService getLeaveTypesService;
        private readonly IGetEmployeesInTeamService getEmployeesInTeamService;
        private readonly IGetOfficialHolidaysService getOfficialHolidaysService;
        private readonly LeaveSystemDbContext db; //ToDo: Later maybe the db should be removed
        public LeavesController(
            IGetLeaveTypesService getLeaveTypesService,
            IGetEmployeesInTeamService getEmployeesInTeamService,
            IGetOfficialHolidaysService getOfficialHolidaysService,
            LeaveSystemDbContext db)
        {
            this.getLeaveTypesService = getLeaveTypesService;
            this.getEmployeesInTeamService = getEmployeesInTeamService;
            this.getOfficialHolidaysService = getOfficialHolidaysService;
            this.db = db;
        }

        public IActionResult Add()
        {
            var model = new AddLeaveFormModel
            {
                LeaveTypes = this.getLeaveTypesService.GetLeaveTypes(),
                EmployeesInTeam = this.getEmployeesInTeamService.GetEmployeesInTeam(),
                ОfficialHolidays = this.getOfficialHolidaysService.GetHolidays(),
            };

            return View(model);
        }

        public IActionResult All(Status? status, string firstName, string lastName, string startDate)
        {
            var leavesQuery = this.db.Leaves.AsQueryable();

            if (status != null)
            {
                leavesQuery = leavesQuery.Where(l => l.Status.ToString().ToLower() == status.ToString().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                leavesQuery = leavesQuery.Where(l => l.RequestEmployee.FirstName.ToLower().Contains(firstName.Trim().ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                leavesQuery = leavesQuery.Where(l => l.RequestEmployee.LastName.ToLower() == lastName.Trim().ToLower());
            }

            var leaves = leavesQuery
                .OrderByDescending(l => l.StartDate)
                .Select(l => new LeaveListingViewModel
                {
                    Id = l.Id,
                    FirstName = l.RequestEmployee.FirstName,
                    LastName = l.RequestEmployee.LastName,
                    StartDate = l.StartDate.ToLocalTime().ToShortDateString(),
                    EndDate = l.EndDate.ToLocalTime().ToShortDateString(),
                    TotalDays = l.TotalDays,
                    Status = l.Status.ToString(),
                    RequestDate = l.RequestDate.ToLocalTime().ToShortDateString()
                })
                .ToList();

            var statuses = Enum.GetValues(typeof(Status))
                .Cast<Status>()
                .ToList();

            return View(new AllLeavesQueryModel
            {
                Leaves = leaves,
                Statuses = statuses,
            });
        }



        [HttpPost]
        public IActionResult Add(AddLeaveFormModel leaveModel)
        {
            if (leaveModel.StartDate > leaveModel.EndDate)
            {
                this.ModelState.AddModelError(nameof(leaveModel.StartDate), "Start date should be before end date.");
                this.ModelState.AddModelError(nameof(leaveModel.EndDate), "End date should be after start date.");

            }

            if (leaveModel.StartDate < DateTime.UtcNow.Date)
            {
                this.ModelState.AddModelError(nameof(leaveModel.StartDate), "Start date should be after or equal to todays' date.");
            }

            if (leaveModel.EndDate < DateTime.UtcNow.Date)
            {
                this.ModelState.AddModelError(nameof(leaveModel.EndDate), "End date should be after or equal to todays' date.");
            }

            var businessDaysCount = GetBusinessDays(leaveModel.StartDate, leaveModel.EndDate);

            if (leaveModel.TotalDays != businessDaysCount || leaveModel.TotalDays == 0)
            {
                this.ModelState.AddModelError(nameof(leaveModel.TotalDays), "Count of days is not correct or it is equal to zero.");
            }

            if (!this.db.LeaveTypes.Any(lt => lt.Id == leaveModel.LeaveTypeId))
            {
                this.ModelState.AddModelError(nameof(leaveModel.LeaveTypeId), "Leave type does not exist.");
            }

            if (!this.db.Teams.Any(t => t.Id == _EmployeeTeamId && t.Employees.Any(e => e.Id == leaveModel.SubstituteEmployeeId))) //ToDo: Change it with current user teamId
            {
                this.ModelState.AddModelError(nameof(leaveModel.SubstituteEmployeeId), "There is no such employee in your team.");
            }


            var employeeLeave = this.db.EmployeesLeaveTypes
                .Include(x => x.LeaveType)
                .Where(el => el.EmployeeId == _EmployeeId &&
                       el.LeaveTypeId == leaveModel.LeaveTypeId)
                .FirstOrDefault(); //ToDo: Change it with current user Id

            if (employeeLeave.RemainingDays == 0 || employeeLeave.RemainingDays < leaveModel.TotalDays)
            {
                this.ModelState.AddModelError(nameof(leaveModel.TotalDays), "You do no have enough days left from the selected leave type option.");
            }


            var leaves = this.db.Leaves.Where(l => l.RequestEmployeeId == _EmployeeId && l.EndDate >= DateTime.UtcNow.Date).ToList();  //ToDo: Change it with current user Id

            for (int i = 0; i < leaves.Count; i++)
            {
                var currentLeave = leaves[i];

                var isStartDateTaken = IsInRange(leaveModel.StartDate, currentLeave.StartDate, currentLeave.EndDate);
                var isEndDateTaken = IsInRange(leaveModel.EndDate, currentLeave.StartDate, currentLeave.EndDate);

                if (isStartDateTaken)
                {
                    this.ModelState.AddModelError(nameof(leaveModel.StartDate), "You already have Leave Request for this date.");
                }

                if (isEndDateTaken)
                {
                    this.ModelState.AddModelError(nameof(leaveModel.EndDate), "You already have Leave Request for this date.");

                }

                if (isStartDateTaken || isEndDateTaken)
                {
                    break;
                }
            }

            var substituteLeaves = this.db.Leaves
                .Where(l => l.SubstituteEmployeeId == _EmployeeId &&
                            l.Status == Status.Approved &&
                            l.EndDate >= DateTime.UtcNow.Date)
                .ToList();  //ToDo: Change it with current user Id

            for (int i = 0; i < substituteLeaves.Count; i++)
            {
                var currentLeave = substituteLeaves[i];

                var isStartDateTaken = IsInRange(leaveModel.StartDate, currentLeave.StartDate, currentLeave.EndDate);
                var isEndDateTaken = IsInRange(leaveModel.EndDate, currentLeave.StartDate, currentLeave.EndDate);

                if (isStartDateTaken)
                {
                    this.ModelState.AddModelError(nameof(leaveModel.StartDate), "You are substitute for this date.");
                }

                if (isEndDateTaken)
                {
                    this.ModelState.AddModelError(nameof(leaveModel.EndDate), "You are substitute for this date.");

                }

                if (isStartDateTaken || isEndDateTaken)
                {
                    break;
                }
            }




            if (!ModelState.IsValid)
            {
                leaveModel.LeaveTypes = this.getLeaveTypesService.GetLeaveTypes();
                leaveModel.EmployeesInTeam = this.getEmployeesInTeamService.GetEmployeesInTeam();
                return View(leaveModel);
            }

            employeeLeave.UsedDays = employeeLeave.UsedDays + leaveModel.TotalDays;
            var approveEmployeeId = db.Employees.Where(e => e.Id == _EmployeeId).Select(e => e.TeamLeadId).FirstOrDefault();

            var leave = new Leave
            {
                StartDate = leaveModel.StartDate.Date,
                EndDate = leaveModel.EndDate.Date,
                TotalDays = leaveModel.TotalDays,
                LeaveTypeId = leaveModel.LeaveTypeId,
                RequestEmployeeId = _EmployeeId, //ToDo: Change it with current user Id
                SubstituteEmployeeId = leaveModel.SubstituteEmployeeId,
                ApproveEmployeeId = approveEmployeeId, //ToDo: Change it with approveEmployeeId
                Comments = leaveModel.Comments,
                RequestDate = leaveModel.RequestedDate
            };

            this.db.Leaves.Add(leave);
            this.db.SaveChanges();

            return RedirectToAction(nameof(All));
        }


        //public IActionResult Preview(AddLeaveFormModel leaveModel)
        //{
        //    return View(leaveModel);
        //    // return RedirectToAction("Index", "Home");
        //}
        private static double GetBusinessDays(DateTime startD, DateTime endD)
        {
            double calcBusinessDays =
                1 + ((endD - startD).TotalDays * 5 -
                (startD.DayOfWeek - endD.DayOfWeek) * 2) / 7;

            if (endD.DayOfWeek == DayOfWeek.Saturday)
            {
                calcBusinessDays--;
            }

            if (startD.DayOfWeek == DayOfWeek.Sunday)
            {
                calcBusinessDays--;
            }

            return calcBusinessDays;
        }

        private static bool IsInRange(DateTime currentDate, DateTime startDate, DateTime endDate)
        {
            return currentDate >= startDate && currentDate <= endDate;
        }

    }

}