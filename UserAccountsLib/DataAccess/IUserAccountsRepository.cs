using System;
using System.Collections.Generic;

using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace UserAccountsLib.DataAccess
{
    // Template for Generic Repositories
    public interface IUserAccountsRepository
    {
        UserAccount Get(string email);
        void Add(UserAccount userAccount);
        void Update(UserAccount userAccount);
        void Delete(UserAccount userAccount);
        void Save();
    }
}
