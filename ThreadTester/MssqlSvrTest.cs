using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;

namespace ThreadTester
{
    public partial class MssqlSvrTest : Form
    {
        //crate a login with useradmin/useradmin sysadmin / dbcreator role

        public static string DbConnectionString => @"Data Source=127.0.0.1\IMPOS;Persist Security Info=True;User ID=useradmin;Password=useradmin;";

        public MssqlSvrTest()
        {
            InitializeComponent();
        }

        private async void btnOpenedConn_Click(object sender, EventArgs e)
        {
            await RunRealCase(false).ConfigureAwait(false);
        }

        private async void btnReal_Click(object sender, EventArgs e)
        {
            await RunRealCase(true).ConfigureAwait(true); //works fine with multiresultset support 
        }


        private static IEnumerable<Customers> GetCustomers(int max = 100)
        {

            return Enumerable.Range(1, max)
                .Select(Customers.CreateCustomer);

        }


        private string GetConnection(string databaseName = "customer_test")
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(DbConnectionString)
            {
                InitialCatalog= databaseName
                , MultipleActiveResultSets = true 
            };

            return connectionStringBuilder.ConnectionString;
        }

        private void CreateDB(string database = "customer_test")
        {

            var sql = @"

CREATE TABLE customers ( 
	id        int           identity(1,1) PRIMARY KEY ,
	firstname                 varchar(100)  NOT NULL,
	lasttname                 varchar(100)  NOT NULL,
	deleted  bit  DEFAULT 'false' NOT NULL,
	CreatedUtc     datetime NULL,
	UpdatedAt      datetime NULL,
	CreatedAtLocalNullable   datetime NULL,
	CreateAtLocal    datetime  NOT NULL,
	version                 varchar(100)  NULL,

 Amount numeric(10,2) DEFAULT 0 NOT NULL	,
 Age int null
	
 );";

            var connection = new SqlConnection(DbConnectionString);

            connection.Open();//if this line is commented then we will get connection is already open .
            //I need to use single connection to use across all these queries to run async // single connection is to manage transaction.

            var name = connection.ExecuteScalar<string>(
                $"SELECT name FROM master.dbo.sysdatabases WHERE name='{database}'");

            if(string.IsNullOrWhiteSpace(name))
            {
                connection.Execute($"create database {database}");

                connection.Close();

                connection = new SqlConnection(GetConnection(database));

                connection.Execute(sql);
            }
            else
            {
                connection.Close();
            }

        }

        private async Task RunRealCase(bool openconn)
        {

            var dbname = "test_db";
            CreateDB(dbname);
            var customers = GetCustomers().ToList();


            //var transaction = db.CreateTransaction();


            var sdate = DateTime.Now.ToString("s");

            
            var connection = new SqlConnection(GetConnection(dbname));

            if(openconn)
            {
                connection.Open();
            }




            var insertSql = @"INSERT INTO customers(
             firstname, lasttname, deleted, createdutc, updatedat, createdatlocalnullable, 
            createatlocal, version, amount, age) values( @firstname, @lastname, @deleted, @createdutc, @updatedat, @createdatlocalnullable, 
            @createatlocal, @version, @amount, @age)
        select SCOPE_IDENTITY() as id";


            //Emulate the task scenario by parallel foreach loop
            var tasks = new List<Task<Customers>>();




            foreach(var i in customers)
            {

                var command = new CommandDefinition(insertSql, i);
                var id = await connection.ExecuteScalarAsync<int>(command).ConfigureAwait(false);


                var selectcommand = new CommandDefinition("select * from customers where id=@id", new { id });
                var task = connection.QueryFirstOrDefaultAsync<Customers>(selectcommand);



                tasks.Add(task);
                Console.WriteLine($"Managed Thread Id: {Thread.CurrentThread.ManagedThreadId}");

            }


            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
