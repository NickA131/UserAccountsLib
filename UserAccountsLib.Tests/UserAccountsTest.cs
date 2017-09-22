using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UserAccountsLib;
using UserAccountsLib.DataAccess;
using Moq;

namespace UserAccountsLib.Tests
{
    [TestClass]
    public class UserAccountsTest
    {
        // Password = "Password";
        // Hash = "e7cf3ef4f17c3999a94f2c6f612e8a888e5b1026878e4e19398b23bd38ec221a"
        private const string TestPassword = "Password";
        private const string HashedPassword = "e7cf3ef4f17c3999a94f2c6f612e8a888e5b1026878e4e19398b23bd38ec221a";

        private Mock<IUserAccountsRepository> _userAccountsRepository;
        private Mock<IHashCalculator> _hashCalculator;
        private Mock<IEmailer> _emailer;
        private UserAccounts _userAccounts;
        
        private string _createHashArgs;
        private UserAccount _testUser;

        #region Test Setup
        [TestInitialize]
        public void TestInitialize()
        {
            _userAccountsRepository = new Mock<IUserAccountsRepository>();
            _hashCalculator = new Mock<IHashCalculator>();
            _emailer = new Mock<IEmailer>();

            _hashCalculator.Setup(h => h.CreateHash(It.IsAny<string>()))
                           .Returns(HashedPassword)
                           .Callback<string>(a => _createHashArgs = a);

            _userAccounts = new UserAccounts(_userAccountsRepository.Object, _hashCalculator.Object, _emailer.Object);

            _testUser = new UserAccount()
            {
                Id = new Guid("CA723BC8-07E2-429C-806A-6D7D062CF86A"),
                FullName = "Fred Jones",
                Email = "Fred.Jones@test.com",
                Password = HashedPassword,
                SecurityToken = new Guid("C233ED0C-EA1F-46F9-B2F3-A0476C20B0FC"),
                Verified = false
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _userAccountsRepository = null;
            _hashCalculator = null;

        }
        #endregion

        #region Register Tests
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Register_MissingEmail_NullArgumentException()
        {
            var userAccountInfo = new UserAccountInfo() { FullName = "John Smith", Password = "Password" };

            _userAccounts.Register(userAccountInfo);

            // Expecting: ArgumentNullException
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Register_MissingFullName_NullArgumentException()
        {
            var userAccountInfo = new UserAccountInfo() { Email = "John.Smith@test.com", Password = "Password" };

            _userAccounts.Register(userAccountInfo);

            // Expecting: ArgumentNullException
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Register_MissingPassword_NullArgumentException()
        {
            var userAccountInfo = new UserAccountInfo() { FullName = "John Smith", Email = "John.Smith@test.com" };

            _userAccounts.Register(userAccountInfo);

            // Expecting: ArgumentNullException
        }

        [TestMethod]
        public void Register_GetUser_ArgumentEmail()
        {
            var userAccountInfo = new UserAccountInfo() { FullName = "John Smith", Email = "John.Smith@test.com", Password = "Password" };

            var getUserArgs = string.Empty;
            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(new UserAccount())
                .Callback<string>(u => getUserArgs = u);

            _userAccounts.Register(userAccountInfo);

            // Password passed to hashing function
            Assert.AreEqual(userAccountInfo.Email, getUserArgs);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void Register_UserExists_ApplicationException()
        {
            var userAccountInfo = new UserAccountInfo() { FullName = "John Smith", Email = "John.Smith@test.com", Password = "Password" };

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);

            _userAccounts.Register(userAccountInfo);

            // Expecting: ApplicationException
        }
        
        [TestMethod]
        public void Register_CreateHash_ArgumentPassword()
        {
            var userAccountInfo = new UserAccountInfo() { FullName = "John Smith", Email = "John.Smith@test.com", Password = "Password" };

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(new UserAccount());

            _userAccounts.Register(userAccountInfo);

            // Password passed to hashing function
            Assert.AreEqual(userAccountInfo.Password, _createHashArgs);
        }

        [TestMethod]
        public void Register_AddUser_ArgumentPopulatedEmailFullnamePasswordHash()
        {
            var userAccountInfo = new UserAccountInfo() { FullName = "John Smith", Email = "John.Smith@test.com", Password = "Password" };

            var addUserArgs = new UserAccount();
            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
               .Returns(new UserAccount());
            _userAccountsRepository.Setup(r => r.Add(It.IsAny<UserAccount>()))
               .Callback<UserAccount>(u => addUserArgs = u);

            _userAccounts.Register(userAccountInfo);

            // Check .Add arguments are properly populated
            Assert.AreEqual(userAccountInfo.FullName, addUserArgs.FullName);
            Assert.AreEqual(userAccountInfo.Email, addUserArgs.Email);
            Assert.AreEqual(HashedPassword, addUserArgs.Password);
        }

        [TestMethod]
        public void Register_AddUser_EmailSent()
        {
            var userAccountInfo = new UserAccountInfo() { FullName = "John Smith", Email = "John.Smith@test.com", Password = "Password" };

            var sendEmailTemplate = EmailTemplates.NONE;
            var email =string.Empty;
            var fullName =string.Empty;
            Guid? securityToken = null;
            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
               .Returns(new UserAccount());
            _userAccountsRepository.Setup(r => r.Add(It.IsAny<UserAccount>()));
            _emailer.Setup(e => e.SendEmail(It.IsAny<EmailTemplates>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
               .Callback<EmailTemplates, string, string, Guid?>((t, e, n, s) => { 
                                                                                    sendEmailTemplate = t;
                                                                                    email = e;
                                                                                    fullName = n;
                                                                                    securityToken = s;
                                                                                    });

            _userAccounts.Register(userAccountInfo);

            // Check .SendEmail arguments are properly populated
            Assert.AreEqual(EmailTemplates.REGISTER, sendEmailTemplate);
            Assert.AreEqual(userAccountInfo.Email, email);
            Assert.AreEqual(userAccountInfo.FullName, fullName);
            Assert.IsInstanceOfType(securityToken, typeof(Guid));
            Assert.AreNotEqual(Guid.Empty, securityToken);

        }
        
        [TestMethod]
        public void Register_AddUser_ArgumentPopulatedAdminFieldsIdSecurityTokenVerified()
        {
            var userAccountInfo = new UserAccountInfo() { FullName = "John Smith", Email = "John.Smith@test.com", Password = "Password" };

            var addUserArgs = new UserAccount();
            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
               .Returns(new UserAccount());
            _userAccountsRepository.Setup(r => r.Add(It.IsAny<UserAccount>()))
               .Callback<UserAccount>(u => addUserArgs = u);

            _userAccounts.Register(userAccountInfo);

            // Check .Add arguments are properly populated
            Assert.IsInstanceOfType(addUserArgs.Id, typeof(Guid));
            Assert.AreNotEqual(Guid.Empty, addUserArgs.Id);
            Assert.IsInstanceOfType(addUserArgs.SecurityToken, typeof(Guid));
            Assert.AreNotEqual(Guid.Empty, addUserArgs.SecurityToken);
            Assert.IsFalse(addUserArgs.Verified);
        }
        #endregion

        #region ConfirmRegistration Tests
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConfirmRegistration_MissingEmail_NullArgumentException()
        {
            string email = null;
            var securityToken = new Guid("804444A0-CB0E-4AB9-9CFD-43DD8C525EAF");

            _userAccounts.ConfirmRegistration(email, securityToken);

            // Expecting: ArgumentNullException
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConfirmRegistration_MissingSecurityToken_NullArgumentException()
        {
            var email = "John.Smith@test.com";
            var securityToken = Guid.Empty;

            _userAccounts.ConfirmRegistration(email, securityToken);

            // Expecting: ApplicationException
        }
        
        [TestMethod]
        public void ConfirmRegistration_GetUser_ArgumentEmail()
        {
            var email = "John.Smith@test.com";
            var securityToken = new Guid("804444A0-CB0E-4AB9-9CFD-43DD8C525EAF");

            var getUserArgs = string.Empty;
            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(new UserAccount() { Id = Guid.NewGuid() })
                .Callback<string>(u => getUserArgs = u);
            
            _userAccounts.ConfirmRegistration(email, securityToken);

            Assert.AreEqual(email, getUserArgs);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void ConfirmRegistration_UserNotFound_ApplicationException()
        {
            var email = "John.Smith@test.com";
            var securityToken = new Guid("804444A0-CB0E-4AB9-9CFD-43DD8C525EAF");

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(new UserAccount());
               
            _userAccounts.ConfirmRegistration(email, securityToken);

            // Expecting: ApplicationException
        }

        [TestMethod]
        public void ConfirmRegistration_UserRegistrationConfirmed_ReturnsTrue()
        {
            var email = _testUser.Email;
            var securityToken = (Guid)_testUser.SecurityToken;

            var getUserArgs = string.Empty;
            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser)
                .Callback<string>(u => getUserArgs = u);

            var result = _userAccounts.ConfirmRegistration(email, securityToken);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ConfirmRegistration_UserRegistrationConfirmed_VerifiedSetSecurityTokenCleared()
        {
            var email = _testUser.Email;
            var securityToken = (Guid)_testUser.SecurityToken;

            var getUserArgs = string.Empty;
            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser)
                .Callback<string>(u => getUserArgs = u);
            var updateUserArgs = new UserAccount();
            _userAccountsRepository.Setup(r => r.Update(It.IsAny<UserAccount>()))
               .Callback<UserAccount>(u => updateUserArgs = u);

            var result = _userAccounts.ConfirmRegistration(email, securityToken);

            Assert.IsTrue(updateUserArgs.Verified);
            Assert.IsNull(updateUserArgs.SecurityToken);
        }

        [TestMethod]
        public void ConfirmRegistration_SecurityTokenDoesntMatch_ReturnsFalse()
        {
            var email = _testUser.Email;
            var securityToken = new Guid("804444A0-CB0E-4AB9-9CFD-43DD8C525EAF");

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);

            var result = _userAccounts.ConfirmRegistration(email, securityToken);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ConfirmRegistration_UserAlreadyVerified_ReturnsFalse()
        {
            var email = _testUser.Email;
            var securityToken = (Guid)_testUser.SecurityToken;

            _testUser.Verified = true;

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);

            var result = _userAccounts.ConfirmRegistration(email, securityToken);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ConfirmRegistration_NoSecurityTokenSet_ReturnsFalse()
        {
            var email = _testUser.Email;
            var securityToken = (Guid)_testUser.SecurityToken;

            _testUser.SecurityToken = null;

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);

            var result = _userAccounts.ConfirmRegistration(email, securityToken);

            Assert.IsFalse(result);
        }
        
        #endregion

        #region Login Tests
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Login_MissingEmail_NullArgumentException()
        {
            var email = string.Empty;
            var password = TestPassword;

            _userAccounts.Login(email, password);

            // Expecting: ArgumentNullException
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Login_MissingPassword_NullArgumentException()
        {
            var email = "John.Smith@test.com";
            var password = string.Empty;

            _userAccounts.Login(email, password);

            // Expecting: ArgumentNullException
        }

        [TestMethod]
        public void Login_GetUser_ArgumentEmail()
        {
            var email = "John.Smith@test.com";
            var password = TestPassword;

            var getUserArgs = string.Empty;
            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser)
                .Callback<string>(u => getUserArgs = u);

            _userAccounts.Login(email, password);

            Assert.AreEqual(email, getUserArgs);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void Login_UserNotFound_ApplicationException()
        {
            var email = "John.Smith@test.com";
            var password = "Password";

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(new UserAccount());

            _userAccounts.Login(email, password);

            // Expecting: ApplicationException
        }

        [TestMethod]
        public void Login_CreateHash_ArgumentPassword()
        {
            var email = _testUser.Email;
            var password = "TestPassword";

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);

            _userAccounts.Login(email, password);

            // Password passed to hashing function
            Assert.AreEqual(password, _createHashArgs);
        }

        [TestMethod]
        public void Login_PasswordDoesntMatch_ReturnsEmptyObject()
        {
            var email = _testUser.Email;
            var password = "DifferentPassword";

            _testUser.Verified = true;
            _testUser.Password = "#DifferentPasswordHash#";

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);
            
            var result = _userAccounts.Login(email, password);

            // Expecting empty object
            Assert.IsNull(result.FullName);
            Assert.IsNull(result.Email);
            Assert.IsNull(result.Password);
        }

        [TestMethod]
        public void Login_UserNotVerified_ReturnsEmptyObject()
        {
            var email = _testUser.Email;
            var password = TestPassword;

            _testUser.Verified = false;

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);

            var result = _userAccounts.Login(email, password);

            // Expecting empty object
            Assert.IsNull(result.FullName);
            Assert.IsNull(result.Email);
            Assert.IsNull(result.Password);
        }

        [TestMethod]
        public void Login_CredentialsValid_ReturnsUserDetails()
        {
            var email = _testUser.Email;
            var password = TestPassword;

            _testUser.Verified = true;

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);

            var result = _userAccounts.Login(email, password);

            // Expecting populated object
            Assert.IsNotNull(result.FullName);
            Assert.AreEqual(email, result.Email); // Emails should match
            Assert.IsNull(result.Password);       // For security reasons pasword hash not returned
        }

        #endregion

        #region ForgotPassword Tests
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ForgotPassword_MissingEmail_NullArgumentException()
        {
            var email = string.Empty;

            _userAccounts.ForgotPassword(email);

            // Expecting: ArgumentNullException
        }

        [TestMethod]
        public void ForgotPassword_GetUser_ArgumentEmail()
        {
            var email = "John.Smith@test.com";
            
            var getUserArgs = string.Empty;
            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(new UserAccount() { Id = Guid.NewGuid() })
                .Callback<string>(u => getUserArgs = u);

            _userAccounts.ForgotPassword(email);

            Assert.AreEqual(email, getUserArgs);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void ForgotPassword_UserNotFound_ApplicationException()
        {
            var email = "John.Smith@test.com";

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(new UserAccount());

            _userAccounts.ForgotPassword(email);

            // Expecting: ApplicationException
        }

        [TestMethod]
        public void ForgotPassword_UserNotVerified_ReturnsFalse()
        {
            var email = _testUser.Email;

            _testUser.Verified = false;

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);

            var result = _userAccounts.ForgotPassword(email);

            // Expecting empty object
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ForgotPassword_UserValid_SecurityTokenSet()
        {
            var email = _testUser.Email;

            _testUser.Verified = true;

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);
            
            var updateUserArgs = new UserAccount();
            _userAccountsRepository.Setup(r => r.Update(It.IsAny<UserAccount>()))
               .Callback<UserAccount>(u => updateUserArgs = u);
            
            var result = _userAccounts.ForgotPassword(email);

            // Expecting security token and true result
            Assert.IsInstanceOfType(updateUserArgs.SecurityToken, typeof(Guid));
            Assert.AreNotEqual(Guid.Empty, updateUserArgs.SecurityToken);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ForgotPassword_UserValid_EmailSent()
        {
            var email = _testUser.Email;

            _testUser.Verified = true;

            var sendEmailTemplate = EmailTemplates.NONE;
            var emailaddress = string.Empty;
            var fullName = string.Empty;
            Guid? securityToken = null;

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);
            _userAccountsRepository.Setup(r => r.Update(It.IsAny<UserAccount>()));
            _emailer.Setup(e => e.SendEmail(It.IsAny<EmailTemplates>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
               .Callback<EmailTemplates, string, string, Guid?>((t, e, n, s) =>
               {
                   sendEmailTemplate = t;
                   emailaddress = e;
                   fullName = n;
                   securityToken = s;
               });

            _userAccounts.ForgotPassword(email);

            // Check .SendEmail arguments are properly populated
            Assert.AreEqual(EmailTemplates.CHANGE_PASSWORD, sendEmailTemplate);
            Assert.AreEqual(_testUser.Email, email);
            Assert.AreEqual(_testUser.FullName, fullName);
            Assert.IsInstanceOfType(securityToken, typeof(Guid));
            Assert.AreNotEqual(Guid.Empty, securityToken);
        }
        #endregion

        #region ResetPassword Tests
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResetPassword_MissingEmail_NullArgumentException()
        {
            string email = null;
            var password = TestPassword;
            var securityToken = new Guid("804444A0-CB0E-4AB9-9CFD-43DD8C525EAF");

            _userAccounts.ResetPassword(email, password, securityToken);

            // Expecting: ArgumentNullException
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResetPassword_MissingPassword_NullArgumentException()
        {
            string email = "John.Smith@test.com";
            string password = null;
            var securityToken = new Guid("804444A0-CB0E-4AB9-9CFD-43DD8C525EAF");

            _userAccounts.ResetPassword(email, password, securityToken);

            // Expecting: ArgumentNullException
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResetPassword_MissingSecurityToken_NullArgumentException()
        {
            var email = "John.Smith@test.com";
            var password = TestPassword;
            var securityToken = Guid.Empty;

            _userAccounts.ResetPassword(email, password, securityToken);

            // Expecting: ApplicationException
        }

        [TestMethod]
        public void ResetPassword_GetUser_ArgumentEmail()
        {
            var email = "John.Smith@test.com";
            var password = TestPassword;
            var securityToken = new Guid("804444A0-CB0E-4AB9-9CFD-43DD8C525EAF");

            var getUserArgs = string.Empty;
            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser)
                .Callback<string>(u => getUserArgs = u);

            _userAccounts.ResetPassword(email, password, securityToken);

            Assert.AreEqual(email, getUserArgs);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void ResetPassword_UserNotFound_ApplicationException()
        {
            var email = "John.Smith@test.com";
            var password = TestPassword;
            var securityToken = new Guid("804444A0-CB0E-4AB9-9CFD-43DD8C525EAF");

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(new UserAccount());

            _userAccounts.ResetPassword(email, password, securityToken);

            // Expecting: ApplicationException
        }

        [TestMethod]
        public void ResetPassword_SecurityTokenDoesntMatch_ReturnsFalse()
        {
            var email = _testUser.Email;
            var password = TestPassword;
            var securityToken = new Guid("804444A0-CB0E-4AB9-9CFD-43DD8C525EAF");

            _testUser.Verified = true;

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);

            var result = _userAccounts.ResetPassword(email, password, securityToken);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ResetPassword_UserNotVerified_ReturnsFalse()
        {
            var email = _testUser.Email;
            var password = TestPassword;
            var securityToken = (Guid)_testUser.SecurityToken;

            _testUser.Verified = false;

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);

            var result = _userAccounts.ResetPassword(email, password, securityToken);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ResetPassword_ResetPasswordConfirmed_SetSecurityTokenCleared()
        {
            // New Password     : N3wPassword
            // New Password Hash: 7e27323e785324ac78a745666913d3b7b764029282a2a2da59459dbc4ca8849d

            var email = _testUser.Email;
            var newPassword = "N3wPassword";
            var newHashedPassword = "7e27323e785324ac78a745666913d3b7b764029282a2a2da59459dbc4ca8849d";
            var securityToken = (Guid)_testUser.SecurityToken;

            var hashCalculator = new Mock<IHashCalculator>();
            hashCalculator.Setup(h => h.CreateHash(It.IsAny<string>()))
                           .Returns(newHashedPassword)
                           .Callback<string>(a => _createHashArgs = a);

            var userAccounts = new UserAccounts(_userAccountsRepository.Object, hashCalculator.Object, _emailer.Object);

            _testUser.Verified = true;
            _testUser.Password = newHashedPassword;

            _userAccountsRepository.Setup(r => r.Get(It.IsAny<string>()))
                .Returns(_testUser);
            var updateUserArgs = new UserAccount();
            _userAccountsRepository.Setup(r => r.Update(It.IsAny<UserAccount>()))
               .Callback<UserAccount>(u => updateUserArgs = u);

            var result = userAccounts.ResetPassword(email, newPassword, securityToken);

            Assert.IsTrue(result);
            Assert.AreEqual(newHashedPassword, updateUserArgs.Password);
            Assert.IsNull(updateUserArgs.SecurityToken);
        }

        #endregion
    }
}
