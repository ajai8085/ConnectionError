using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using static ThreadTester.StackExchangeRedisExtensions;
using Dapper;

namespace ThreadTester
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public static string DbConnectionString => "Server=localhost;Port=5432;User Id=postgres;Password=pwd; Pooling=false;";

        private string GetConnection(string databaseName= "customer_test")
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(DbConnectionString)
            {
                Database = databaseName
            };

            return connectionStringBuilder.ConnectionString;
        }

        private void CreateDB(string database= "customer_test")
        {

            var sql = @"

CREATE TABLE customers ( 
	id                   SERIAL PRIMARY KEY ,
	firstname                 varchar(100)  NOT NULL,
	lasttname                 varchar(100)  NOT NULL,
	deleted bool DEFAULT false NOT NULL,
	CreatedUtc timestamp without time zone default (now() at time zone 'utc')   NOT NULL,
	UpdatedAt  timestamp without time zone default (now() at time zone 'utc')   NOT NULL,
	CreatedAtLocalNullable   timestamp without time zone default (now() at time zone 'utc')   NULL,
	CreateAtLocal    timestamp without time zone default (now() at time zone 'utc')   NOT NULL,
	version                 varchar(100)  NULL,

 Amount numeric(10,2) DEFAULT 0 NOT NULL	,
 Age int null
	
 );";

            var connection = new NpgsqlConnection(DbConnectionString);

            connection.Open();//if this line is commented then we will get connection is already open .
            //I need to use single connection to use across all these queries to run async // single connection is to manage transaction.

            var name =connection.ExecuteScalar<string>(
                $"SELECT datname FROM pg_database WHERE datistemplate = false and datname='{database}'");

            if (string.IsNullOrWhiteSpace(name))
            {
                connection.Execute($"create database {database}");

                connection.Close();

                connection = new NpgsqlConnection(GetConnection(database));

                connection.Execute(sql);
            }
            else
            {
                connection.Close();
            }
            
        }

        private ICacheFactory _cacheFactory;

        public ICacheFactory CacheFactory()
        {
            if (_cacheFactory == null)
            {
                _cacheFactory = new CacheFactory();
            }

            return _cacheFactory;
        }

        private async void button1_Click(object sender, EventArgs e)
        {

            Console.WriteLine($"Managed Thread Id: {Thread.CurrentThread.ManagedThreadId}");

            //await WriteAsync();

            await WriteAsync();

            Console.WriteLine($"Managed Thread Id: {Thread.CurrentThread.ManagedThreadId}");

            MessageBox.Show("completed!");

            Console.WriteLine($"Managed Thread Id: {Thread.CurrentThread.ManagedThreadId}");

            await Task.Delay(TimeSpan.FromSeconds(20));

            Console.WriteLine($"Managed Thread Id: {Thread.CurrentThread.ManagedThreadId}");
        }


        public async Task WriteAsync()
        {
            var dbname = "test_db";
            CreateDB(dbname);
            var customers = GetCustomers().ToList();
            

            //var transaction = db.CreateTransaction();


            var sdate = DateTime.Now.ToString("s");


            var connection = new NpgsqlConnection(GetConnection(dbname));
            //connection.Open();



            var insertSql = @"INSERT INTO customers(
             firstname, lasttname, deleted, createdutc, updatedat, createdatlocalnullable, 
            createatlocal, version, amount, age) values( @firstname, @lasttname, @deleted, @createdutc, @updatedat, @createdatlocalnullable, 
            @createatlocal, @version, @amount, @age) RETURNING ID";


            //Emulate the task scenario by parallel foreach loop
            var tasks = new List<Task<Customers>>();



            Parallel.ForEach(customers, async i =>
            {
                var command = new CommandDefinition(insertSql, i);
                var id = await connection.ExecuteScalarAsync<int>(command).ConfigureAwait(false);


                var selectcommand = new CommandDefinition("select * from customers where id=@id", new {id});
                var task = connection.QueryFirstOrDefaultAsync<Customers>(selectcommand);



                tasks.Add(task);
                Console.WriteLine($"Managed Thread Id: {Thread.CurrentThread.ManagedThreadId}");

            });


            await Task.WhenAll(tasks).ConfigureAwait(false);


            return;
            //redis 
            


            var cf = CacheFactory();

            var db = cf.GetDatabase();

            var redisTasks = new List<Task>();
            for (var i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];
                var c = await t;

                var key = $"Customer:{c.Id}";
                var hash = ConvertToHashEntryList(i).ToArray();
                var task = db.HashSetAsync(key, hash);

                redisTasks.Add(task);


            }

            await Task.WhenAll(redisTasks).ConfigureAwait(false);

            //await transaction.ExecuteAsync().ConfigureAwait(false);




            Console.WriteLine($"Managed Thread Id: {Thread.CurrentThread.ManagedThreadId}");
            //await Task.WhenAll(tasks).ConfigureAwait(false);

            Console.WriteLine($"END FN Managed Thread Id: {Thread.CurrentThread.ManagedThreadId}");
        }




        private static IEnumerable<Customers> GetCustomers(int max = 100)
        {

            return Enumerable.Range(1, max)
                .Select(Customers.CreateCustomer);

        }

        private async void button2_Click(object sender, EventArgs e)
        {

            var dbname = "test_db";
            CreateDB(dbname);
            var customers = GetCustomers().ToList();


            //var transaction = db.CreateTransaction();


            var sdate = DateTime.Now.ToString("s");


            var connection = new NpgsqlConnection(GetConnection(dbname));
            //connection.Open();



            var insertSql = @"INSERT INTO customers(
             firstname, lasttname, deleted, createdutc, updatedat, createdatlocalnullable, 
            createatlocal, version, amount, age) values( @firstname, @lasttname, @deleted, @createdutc, @updatedat, @createdatlocalnullable, 
            @createatlocal, @version, @amount, @age) RETURNING ID";


            //Emulate the task scenario by parallel foreach loop
            var tasks = new List<Task<Customers>>();




            foreach (var i in customers)
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


    public class Customers : IEquatable<Customers>
    {
        public Customers()
        {

        }

        private Customers(int id)
        {
            Id = id;
            FirstName = "FirstName " + new Random().NextDouble();
            LastName = "LastName " + new Random().NextDouble();
            Age = new Random().Next(100);
            CreatedUtc = DateTime.UtcNow;
            Amount = (decimal) new Random().NextDouble();
            Deleted = (new Random().Next() % 2) == 0;
            Version = Guid.NewGuid().ToString();

            CreatedAtLocalNullable = DateTime.Now;
            CreateAtLocal = DateTime.Now;

            if (Deleted)
            {
                UpdatedAt = DateTime.Now;
            }
        }

        public static Customers CreateCustomer(int id)
        {
            return new Customers(id);
        }

        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int Age { get; set; }
        public decimal Amount { get; set; }
        public bool Deleted { get; set; }
        public string Version { get; set; }

        public DateTime? CreatedAtLocalNullable { get; set; }

        public DateTime CreateAtLocal { get; set; }

        public bool Equals(Customers other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Id == other.Id && string.Equals(FirstName, other.FirstName) &&
                   string.Equals(LastName, other.LastName) && CreatedUtc.Equals(other.CreatedUtc) && Age == other.Age &&
                   Amount == other.Amount && Deleted == other.Deleted && string.Equals(Version, other.Version);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((Customers) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (FirstName != null ? FirstName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (LastName != null ? LastName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CreatedUtc.GetHashCode();
                hashCode = (hashCode * 397) ^ Age;
                hashCode = (hashCode * 397) ^ Amount.GetHashCode();
                hashCode = (hashCode * 397) ^ Deleted.GetHashCode();
                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"Id:{Id} Fname:{FirstName} Lname:{LastName} Age:{Age} version:{Version}";
        }

        public static bool operator ==(Customers left, Customers right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Customers left, Customers right)
        {
            return !Equals(left, right);
        }
    }
}