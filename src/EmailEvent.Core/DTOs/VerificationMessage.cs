using System;
using System.Collections.Generic;
using System.Text;

namespace EmailEvent.Core.DTOs;

public class VerificationMessage
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
