using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAccountsLib
{
    public interface IUserAccounts
    {
        void Register(UserAccountInfo userAccountInfo);
        bool ConfirmRegistration(string email, Guid securityToken);
        UserAccountInfo Login(string email, string password);
        bool ForgotPassword(string email);
        bool ResetPassword(string email, string password, Guid securityToken);
        
    }
}
