﻿namespace AnnualLeaveSystem.Data.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Leave
    {
        public int Id { get; init; }

        [Column(TypeName = "date")]
        public DateTime RequestDate { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime EndDate { get; set; }

        public int TotalDays { get; set; }

        [Required]
        public int LeaveTypeId { get; set; }

        public LeaveType LeaveType { get; set; }

        [Required]
        public int RequestEmployeeId { get; set; }

        public Employee RequestEmployee { get; set; }

        [Required]
        public int SubstituteEmployeeId { get; set; }

        public Employee SubstituteEmployee { get; set; }


        public int? ApproveEmployeeId { get; set; }

        public Employee ApproveEmployee { get; set; }

        public bool IsApproved { get; set; } = false;

        public bool IsCancelled { get; set; } = false;

        public string Comments { get; set; }
    }
}
