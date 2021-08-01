﻿namespace AnnualLeaveSystem.Services.Emails
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public interface IEmailSenderService
    {
        void SendEmail(string subject, string content);
    }
}
