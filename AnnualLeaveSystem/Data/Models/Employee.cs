﻿namespace AnnualLeaveSystem.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using static AnnualLeaveSystem.Data.DataConstants;
    public class Employee
    {
        public int Id { get; init; }

        [Required]
        [MaxLength(EmployeeFirstNameMaxLength)]
        public string FirstName { get; set; }

        [MaxLength(EmployeeMiddleNameMaxLength)]
        public string MiddleName { get; set; }

        [Required]
        [MaxLength(EmployeeLastNameMaxLength)]
        public string LastName { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        [Required]
        [MaxLength(EmployeeJobTitleMaxLength)]
        public string JobTitle { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        public Department Department { get; set; }

        public int? TeamLeadId { get; set; }

        public Employee TeamLead { get; set; }

        [Required]
        public int TeamId { get; set; }

        public Team Team { get; set; }

        public DateTime HireDate { get; set; }

        [InverseProperty("RequestEmployee")]
        public virtual ICollection<Leave> RequestedLeaves { get; init; } = new HashSet<Leave>();

        [InverseProperty("ApproveEmployee")]
        public virtual ICollection<Leave> ApprovedLeaves { get; init; } = new HashSet<Leave>();

    }
}
