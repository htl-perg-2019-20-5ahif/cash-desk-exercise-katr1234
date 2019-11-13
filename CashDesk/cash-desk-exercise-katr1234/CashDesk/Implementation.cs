using CashDesk.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CashDesk.Model.Model;

namespace CashDesk
{
    /// <inheritdoc />
    public class DataAccess : IDataAccess
    {
        private CashDeskDbContext context;

        /// <inheritdoc />
        public Task InitializeDatabaseAsync() {
            if(context == null)
            {
                throw new InvalidOperationException("You have already initialized it");
            }

            context = new CashDeskDbContext();
            return Task.CompletedTask;
            
        }

        /// <inheritdoc />
        public async Task<int> AddMemberAsync(string firstName, string lastName, DateTime birthday)
        {
            if (context == null)
            {
                throw new InvalidOperationException("Not initialized");
            }

            if (string.IsNullOrEmpty(firstName))
            {
                throw new ArgumentException("Firstname has to be not null or not empty");
            }

            if (string.IsNullOrEmpty(lastName))
            {
                throw new ArgumentException("Lastname has to be not null or not empty");
            }
            if (await context.Members.AnyAsync(m => m.LastName == lastName))
            {
                throw new DuplicateNameException("This Lastname already exists in database");
            }
            var newMember = new Member
            {
                FirstName = firstName,
                LastName = lastName,
                Birthday = birthday
            };
            context.Members.Add(newMember);
            await context.SaveChangesAsync();
            return newMember.MemberNumber;
        }

        /// <inheritdoc />
        public async Task DeleteMemberAsync(int memberNumber)
        {
            if (context == null)
            {
                throw new InvalidOperationException("Not initialized");
            }

            Member memberToRemove;
            try
            {
                memberToRemove = await context.Members.FirstAsync(m => m.MemberNumber == memberNumber);
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException();
            }
            context.Members.Remove(memberToRemove);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<IMembership> JoinMemberAsync(int memberNumber)
        {
            if (context == null)
            {
                throw new InvalidOperationException("Not initialized");
            }
            if (await context.Memberships.AnyAsync(m => m.Member.MemberNumber == memberNumber
              && m.End == DateTime.MaxValue))
            {
                throw new AlreadyMemberException();
            }
            var membership = new Membership
            {
                Member = await context.Members.FirstAsync(m => m.MemberNumber == memberNumber),
                Begin = DateTime.Now,
                End = DateTime.MaxValue
            };
            context.Memberships.Add(membership);
            await context.SaveChangesAsync();
            return membership;
        }

        /// <inheritdoc />
        public async Task<IMembership> CancelMembershipAsync(int memberNumber) {

            if (context == null)
            {
                throw new InvalidOperationException("Not initialized");
            }
            Membership membership;
            try
            {
                membership = await context.Memberships.FirstAsync(m => m.Member.MemberNumber == memberNumber
                    && m.End == DateTime.MaxValue);
            }
            catch (InvalidOperationException)
            {
                throw new NoMemberException();
            }
            membership.End = DateTime.Now;
            await context.SaveChangesAsync();
            return membership;
        }

        /// <inheritdoc />
        public async Task DepositAsync(int memberNumber, decimal amount)
        {
            if (context == null)
            {
                throw new InvalidOperationException("Not initialized");
            }
            Member member;
            try
            {
                member = await context.Members.FirstAsync(m => m.MemberNumber == memberNumber);
            }
            catch (InvalidOperationException)
            {
                throw new NoMemberException();
            }
            Membership membership;
            try
            {
                membership = await context.Memberships.FirstAsync(m => m.Member.MemberNumber == memberNumber
                    && m.End >= DateTime.Now);
            }
            catch (InvalidOperationException)
            {
                throw new NoMemberException();
            }
            var deposit = new Deposit { Membership = membership, Amount = amount };
            context.Deposits.Add(deposit);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IDepositStatistics>> GetDepositStatisticsAsync()
        {
            if (context == null)
            {
                throw new InvalidOperationException("Not initialized");
            }
            return (await context.Deposits.Include("Membership.Member").ToArrayAsync())
                .GroupBy(d => new { d.Membership.Begin.Year, d.Membership.Member })
                .Select(i => new DepositStatistics
                {
                    Year = i.Key.Year,
                    Member = i.Key.Member,
                    TotalAmount = i.Sum(d => d.Amount)
                });
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (context != null)
            {
                context.Dispose();
                context = null;
            }
        }
    }
}
