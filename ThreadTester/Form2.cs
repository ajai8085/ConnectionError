using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
   using Devart.Data.PostgreSql;


namespace ThreadTester
{
    public partial class Form2 : Form
    {

        public static string DbConnectionString => "Server=localhost;Port=5432;User Id=postgres;Password=pwd; Pooling=false;";

        private string GetConnection(string databaseName = "customer_test")
        {
            var connectionStringBuilder = new PgSqlConnectionStringBuilder(DbConnectionString)
            {
                Database = databaseName,
                Pooling = true
            };

            return connectionStringBuilder.ConnectionString;
        }

        private void CreateDB(string database = "customer_test")
        {

            var sql = @"

CREATE TABLE customers ( 
	id                   SERIAL PRIMARY KEY ,
	firstname                 varchar(100)  NOT NULL,
	lasttname                 varchar(100)  NOT NULL,
	deleted bool DEFAULT false NOT NULL,
	CreatedUtc timestamp without time zone default (now() at time zone 'utc')    NULL,
	UpdatedAt  timestamp without time zone default (now() at time zone 'utc')    NULL,
	CreatedAtLocalNullable   timestamp without time zone default (now() at time zone 'utc')   NULL,
	CreateAtLocal    timestamp without time zone default (now() at time zone 'utc')   NOT NULL,
	version                 varchar(100)  NULL,

 Amount numeric(10,2) DEFAULT 0 NOT NULL	,
 Age int null
	
 );";

            

            var connection = new PgSqlConnection(DbConnectionString);

            connection.Open();//if this line is commented then we will get connection is already open .
            //I need to use single connection to use across all these queries to run async // single connection is to manage transaction.

            var name = connection.ExecuteScalar<string>(
                $"SELECT datname FROM pg_database WHERE datistemplate = false and datname='{database}'");

            if(string.IsNullOrWhiteSpace(name))
            {
                connection.Execute($"create database {database}");

                connection.Close();

                connection = new PgSqlConnection(GetConnection(database));

                connection.Execute(sql);
            }
            else
            {
                connection.Close();
            }

        }

        public Form2()
        {
            InitializeComponent();
        }

        private static IEnumerable<Customers> GetCustomers(int max = 100)
        {

            return Enumerable.Range(1, max)
                .Select(Customers.CreateCustomer);

        }


        private async Task RunRealCase(bool openconn)
        {

            var dbname = "test_db";
            CreateDB(dbname);
            var customers = GetCustomers().ToList();


            //var transaction = db.CreateTransaction();


            var sdate = DateTime.Now.ToString("s");

            

            var connection = new PgSqlConnection(GetConnection(dbname));

            if(openconn)
            {
                connection.Open();
            }


            PgSqlCommand cmd = new PgSqlCommand();
            

            var insertSql = @"INSERT INTO customers(
             firstname, lasttname, deleted, createdutc, updatedat, createdatlocalnullable, 
            createatlocal, version, amount, age) values( @firstname, @lastname, @deleted, @createdutc, @updatedat, @createdatlocalnullable, 
            @createatlocal, @version, @amount, @age) ";


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

        private async  void btnReal_Click(object sender, EventArgs e)
        {
            await RunRealCase(true).ConfigureAwait(true); //not working.
        }
    }
}
