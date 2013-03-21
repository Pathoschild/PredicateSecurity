using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using Pathoschild.PredicateSecurity.Tests.Models;

namespace Pathoschild.PredicateSecurity.Tests
{
	[TestFixture(Description = "Asserts that the PredicateFilter behaves as expected when used on NHibernate deferred queries.")]
	public class NHibernateTests : AbstractTests
	{
		/*********
		** Properties
		*********/
		/// <summary>The NHibernate factory which constructs the <see cref="Session"/>.</summary>
		protected ISessionFactory SessionFactory { get; set; }

		/// <summary>The NHibernate session through which to access data.</summary>
		protected ISession Session { get; set; }


		/*********
		** Public methods
		*********/
		/// <summary>Before each unit test, reinitialize the SQL database and NHibernate connection.</summary>
		[SetUp]
		public void Setup()
		{
			Console.WriteLine("Initializing NHibernate\n===============");

			// read NHibernate configuration
			Console.WriteLine("Building session factory...");
			Configuration cfg = new Configuration().Configure();
			this.SessionFactory = cfg.BuildSessionFactory();

			// recreate database
			Console.WriteLine("Recreating database...");
			SchemaExport export = new SchemaExport(cfg);
			export.Drop(false, true);
			export.Create(false, true);

			// insert data
			Console.WriteLine("Inserting sample data...");
			using (ISession session = this.SessionFactory.OpenSession())
			{
				IDictionary<string, User> users = base.GetTestUsers();
				foreach (User user in users.Values)
					session.Save(user);
				foreach (BlogPost post in base.GetTestPosts(users))
					session.Save(post);
			}

			// create session
			this.Session = this.SessionFactory.OpenSession();
			Console.WriteLine("\n\nRunning unit test\n===============");
		}

		/// <summary>After each unit test, dispose the NHibernate connection.</summary>
		[TearDown]
		public void TearDown()
		{
			Console.WriteLine("\n\nClosing connections\n===============");
			if (this.SessionFactory != null)
				this.SessionFactory.Dispose();
			if (this.Session != null)
				this.Session.Dispose();
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Get the sample content with which to test security rules.</summary>
		/// <param name="users">The sample users.</param>
		protected override IQueryable<BlogPost> GetTestPosts(IDictionary<string, User> users)
		{
			return this.Session.Query<BlogPost>();
		}
	}
}