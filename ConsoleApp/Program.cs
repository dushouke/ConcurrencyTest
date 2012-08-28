using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Transactions;
using System.Data.Common;
using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--------------SingleOperation------------------");
            Enumerable.Range(0, 20).ToList().ForEach(i =>
            {
                Helper.MockMulit(Helper.SingleOperation);
            });
            Console.WriteLine("--------------SingleOperation------------------");


            Console.WriteLine("--------------WithUPDLOCK------------------");
            Enumerable.Range(0, 20).ToList().ForEach(i =>
            {
                Helper.MockMulit(Helper.WithUPDLOCK);
            });
            Console.WriteLine("--------------WithUPDLOCK------------------");

            Console.ReadLine();
        }
    }
    public static class Helper
    {
        public static void MockMulit(Action<string> action)
        {
            int threadCount = 100;

            ClearData();

            var tasks = new List<Task>(threadCount);
            Enumerable.Range(1, threadCount).ToList().ForEach(i =>
            {
                var j = i;
                tasks.Add(Task.Factory.StartNew(() => action(string.Format("Thread{0}-{1}", Thread.CurrentThread.ManagedThreadId, j))));
            });
            Task.WaitAll(tasks.ToArray());

            //display result 
            var resultCount = CountData();
            if (resultCount > 20)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else
            {
                Console.ResetColor();
            }
            Console.WriteLine(resultCount);

        }
        public static void SingleOperation(string name)
        {
            using (var conn = GetOpendConn())
            {
                conn.Execute(@"
	                  insert dbo.Down (UserName)
					   select @UserName
						  where (select count(1) from dbo.Down) < 20", new { UserName = name });
            }
        }


        public static void WithUPDLOCK(string name)
        {
            TransactionOptions options = new TransactionOptions();
            options.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
            options.Timeout = TransactionManager.DefaultTimeout;
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, options))
            using (var conn = GetOpendConn())
            {
                var count = conn.Query<int>(@"select count(*)  from dbo.Down with (UPDLOCK)").Single();
                if (count < 20)
                {
                    conn.Execute(@"insert into dbo.Down (UserName) values (@UserName)", new { UserName = name });
                }
                scope.Complete();
            }
        }
        private static void ClearData()
        {
            using (var conn = GetOpendConn())
            {
                conn.Execute(@"delete from dbo.Down");
            }
        }

        private static int CountData()
        {
            using (var conn = GetOpendConn())
            {
                return conn.Query<int>(@"select count(*)  from dbo.Down").Single();
            }
        }

        private static DbConnection GetOpendConn()
        {
            var conn = new SqlConnection(@"Data Source=.;Integrated Security=SSPI;Initial Catalog=ConcurrencyTest;");
            if (conn.State != ConnectionState.Open)
                conn.Open();
            return conn;
        }
    }
}
