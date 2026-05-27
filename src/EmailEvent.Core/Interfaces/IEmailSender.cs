using System;
using System.Collections.Generic;
using System.Text;

namespace EmailEvent.Core.Interfaces;

public interface IEmailSender
{
    Task SendVerificationCodeAsync(string email, string code, CancellationToken ct);
}
