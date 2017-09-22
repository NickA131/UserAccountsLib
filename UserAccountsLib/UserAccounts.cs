using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserAccountsLib.DataAccess;

namespace UserAccountsLib
{
    public class UserAccounts : IUserAccounts
    {

        private IUserAccountsRepository _userAccountsRepository;
        private IHashCalculator _hashCalculator;
        private IEmailer _emailer;

        public UserAccounts(IUserAccountsRepository userAccountsRepository, IHashCalculator hashCalculator, IEmailer emailer)
        {
            _userAccountsRepository = userAccountsRepository;
            _hashCalculator = hashCalculator;
            _emailer = emailer;
        }
                
        public void Register(UserAccountInfo userAccountInfo)
        {
            if (userAccountInfo == null || string.IsNullOrEmpty(userAccountInfo.FullName)
                || string.IsNullOrEmpty(userAccountInfo.Email) || string.IsNullOrEmpty(userAccountInfo.Password))
                throw new ArgumentNullException("UserAccountInfo is missing one or more of Email, Password or FullName.");

            var userAccount = _userAccountsRepository.Get(userAccountInfo.Email);

            // User account object returned is neither empty nor has an empty id
            if (userAccount != null && userAccount.Id != Guid.Empty)
                throw new ApplicationException("User already exists for specified email address.");

            userAccount = new UserAccount(){
                Id = Guid.NewGuid(),
                FullName = userAccountInfo.FullName,
                Email = userAccountInfo.Email,
                Password = _hashCalculator.CreateHash(userAccountInfo.Password),  // Simple hash of password. Can increase security using salts.
                Verified = false,
                SecurityToken = Guid.NewGuid()
            };

            _userAccountsRepository.Add(userAccount);

            //sendEmail
            _emailer.SendEmail(EmailTemplates.REGISTER, userAccount.Email, userAccount.FullName, userAccount.SecurityToken);

        }

        public bool ConfirmRegistration(string email, Guid securityToken)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException("Email is null or empty.");
            if (securityToken == Guid.Empty)
                throw new ArgumentNullException("SecurityToken is empty.");

            var userAccount = _userAccountsRepository.Get(email);

            if (userAccount == null || userAccount.Id == Guid.Empty)
                throw new ApplicationException("User not found for specified email address.");

            if (userAccount.SecurityToken == securityToken && !userAccount.Verified)
            {
                userAccount.SecurityToken = null;  // Clear security token
                userAccount.Verified = true;
                _userAccountsRepository.Update(userAccount);
                return true;
            }
            
            return false;
        }

        public UserAccountInfo Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException("Email is null or empty.");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("Password is null or empty.");

            var userAccount = _userAccountsRepository.Get(email);

            if (userAccount == null || userAccount.Id == Guid.Empty)
                throw new ApplicationException("User not found for specified email address.");

            var userAccountInfo = new UserAccountInfo();
            if(userAccount.Password.Equals(_hashCalculator.CreateHash(password))  && userAccount.Verified)
                userAccountInfo = new UserAccountInfo() {
                Email = userAccount.Email,
                FullName = userAccount.FullName
                // Don't need to return hashed password
            };

            return userAccountInfo; // Empty class indicates failure
        }

        public bool ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException("Email is null or empty.");

            var userAccount = _userAccountsRepository.Get(email);

            if (userAccount == null || userAccount.Id == Guid.Empty)
                throw new ApplicationException("User not found for specified email address.");

            if (userAccount.Verified) // Only allow forgot password if user account is verified
            {
                userAccount.SecurityToken = Guid.NewGuid();
                _userAccountsRepository.Update(userAccount);

                // Send email
                _emailer.SendEmail(EmailTemplates.CHANGE_PASSWORD, userAccount.Email, userAccount.FullName, userAccount.SecurityToken);

                return true;
            }

            return false;
        }

        public bool ResetPassword(string email, string password, Guid securityToken)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException("Email is null or empty.");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("Password is null or empty.");
            if (securityToken == Guid.Empty)
                throw new ArgumentNullException("SecurityToken is empty.");

            var userAccount = _userAccountsRepository.Get(email);

            if (userAccount == null || userAccount.Id == Guid.Empty)
                throw new ApplicationException("User not found for specified email address.");

            if (userAccount.SecurityToken == securityToken && userAccount.Verified) // Only allow reset password if user account is verified
            {
                userAccount.SecurityToken = null;
                userAccount.Password = _hashCalculator.CreateHash(password);
                _userAccountsRepository.Update(userAccount);
                return true;
            }
            
            return false;
        }

    }
}
