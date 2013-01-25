namespace Ecng.Test.Data
{
	#region Using Directives

	using System;
	using System.Data.SqlClient;

	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class Customer
	{
		#region Id

		[Identity]
		[Field("Id", ReadOnly = true)]
		private long _id;

		public long Id
		{
			get { return _id; }
			set { _id = value; }
		}

		#endregion

		#region Name

		[Field("Name")]
		private string _name;

		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		#endregion
	}

	[TestClass]
	public class MappingTest
	{
		[TestMethod]
		public void AdoNetCustomer()
		{
			using (SqlConnection con = new SqlConnection(Config.ConnectionString))
			{
				Customer customer = new Customer();
				customer.Name = "John Smith";

				con.Open();

				// Creating row in database about new costomer
				using (SqlCommand cmd = new SqlCommand(@"	insert into Customer (Name) values (@name);
															select scope_identity()", con))
				{
					cmd.Parameters.AddWithValue("@name", "John Smith");
					customer.Id = cmd.ExecuteScalar().To<long>();
					Console.WriteLine("New customer id: {0}", customer.Id);
				}

				// Update customer
				using (SqlCommand cmd = new SqlCommand("update Customer set Name = @name where Id = @id", con))
				{
					customer.Name = "Mark Twain";

					cmd.Parameters.AddWithValue("@id", customer.Id);
					cmd.Parameters.AddWithValue("@name", customer.Name);

					cmd.ExecuteNonQuery();
				}

				// Select customer from database by id
				using (SqlCommand cmd = new SqlCommand("select Id, Name from Customer where Id = @id", con))
				{
					cmd.Parameters.AddWithValue("@id", customer.Id);

					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						reader.Read();

						customer.Id = (long)reader["Id"];
						customer.Name = (string)reader["Name"];
					}

					Console.WriteLine("Customer id: {0} Customer Name: {1}", customer.Id, customer.Name);
				}

				// Delete customer
				using (SqlCommand cmd = new SqlCommand("delete from Customer where Id = @id", con))
				{
					cmd.Parameters.AddWithValue("@id", customer.Id);
					cmd.ExecuteNonQuery();
				}
			}
		}

		[TestMethod]
		public void EFDCustomer()
		{
			using (Database db = Config.CreateDatabase())
			{
				Customer customer = new Customer();
				customer.Name = "John Smith";

				db.Create(customer);
				Console.WriteLine("New customer id: {0}", customer.Id);

				customer.Name = "Mark Twain";
				db.Update(customer);

				db.ClearCache();
				customer = db.Read<Customer>(customer.Id);
				Console.WriteLine("Customer id: {0} Customer Name: {1}", customer.Id, customer.Name);

				db.Delete(customer);
			}
		}
	}
}