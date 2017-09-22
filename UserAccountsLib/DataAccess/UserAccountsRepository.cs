using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Linq.Expressions;
using System.Data.Entity.Infrastructure;
using System.Text;
using System.Threading.Tasks;

namespace UserAccountsLib.DataAccess
{
    public class UserAccountsRepository: IUserAccountsRepository
    {
        protected DbContext DbContext { get; set;}
        protected DbSet<UserAccount> DbSet { get; set; }
        
        public UserAccountsRepository(DbContext dbContext)
        {
            if (dbContext == null) 
                throw new ArgumentNullException("Null DbContext");
            DbContext = dbContext;
        }

        public UserAccount Get(string email)
        {
            
            if (email == null)
                throw new ArgumentNullException("Email is null");

            var dbQuery = DbContext.Set<UserAccount>().AsQueryable();

            var item = dbQuery.Where(u => u.Email.Equals(email)).FirstOrDefault();

            return item;
        }

        public void Add(UserAccount userAccount)
        {
            if (userAccount == null)
                throw new ArgumentNullException("UserAccount is null");
            
            DbContext.Entry(userAccount).State = EntityState.Added;
            Save();
        }

        public void Update(UserAccount userAccount)
        {
            if (userAccount == null)
                throw new ArgumentNullException("UserAccount is null");

            DbContext.Entry(userAccount).State = EntityState.Modified;
            Save();
        }

        public void Delete(UserAccount userAccount)
        {
            if (userAccount == null)
                throw new ArgumentNullException("UserAccount is null");

            DbContext.Entry(userAccount).State = EntityState.Deleted;
            Save();
        }

        public void Save()
        {
            DbContext.SaveChanges();
        }

    }
}
