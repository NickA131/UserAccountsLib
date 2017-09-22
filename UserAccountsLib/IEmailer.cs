using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAccountsLib
{
    public interface IEmailer
    {
        void SendEmail(EmailTemplates emailTemplate, string email, string name, Guid? securityToken);

    }
}
