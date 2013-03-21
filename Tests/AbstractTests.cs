using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pathoschild.PredicateSecurity.Tests.Models;

namespace Pathoschild.PredicateSecurity.Tests
{
	/// <summary>Provides generic unit tests agnostic of the underlying datasource</summary>
	public abstract class AbstractTests
	{
		/*********
		** Unit tests
		*********/
		[Test(Description = "Assert that the predicate filter correctly filters a collection based on a set of security rules.")]
		[TestCase("editor", "post-approve", Result = "The most ambitious post")]
		[TestCase("editor", "post-edit", Result = "The most ambitious post")]
		[TestCase("submitter", "post-approve", Result = "")]
		[TestCase("submitter", "post-edit", Result = "The best post, The most ambitious post")]
		[TestCase("evenAdmin", "post-approve", Result = "The most ambitious post, The abandoned post")]
		[TestCase("evenAdmin", "post-edit", Result = "The most ambitious post, The abandoned post")]
		public string Filter_CorrectlyAppliesRelativePredicates(string user, string permission)
		{
			// set up content
			IDictionary<string, User> users = this.GetTestUsers();
			IQueryable<BlogPost> posts = this.GetTestPosts(users);

			// set up security groups
			PredicateFilter<int> filter = new PredicateFilter<int>();
			filter.AddGroup<BlogPost>("post-submitter", (post, userID) => post.Submitter != null && post.Submitter.ID == userID);
			filter.AddGroup<BlogPost>("post-editor", (post, userID) => post.Editor != null && post.Editor.ID == userID);
			filter.AddGroup<BlogPost>("post-administrator-for-even-ids", (post, userID) => userID == 4 && post.ID % 2 == 0);

			// set up permission rules:
			// submitters can edit their own posts, but are never allowed to approve them
			// editors can edit and approve posts they're the editors for (as long as they're not submitters)
			// and post-administrators-for-even-ids can edit and approve any post with an even ID (as long as they're not the submitter)
			filter.AddPermission("post-submitter", "post-edit", PermissionValue.Allow);
			filter.AddPermission("post-submitter", "post-approve", PermissionValue.Deny);
			filter.AddPermission("post-editor", "post-edit", PermissionValue.Allow);
			filter.AddPermission("post-editor", "post-approve", PermissionValue.Allow);
			filter.AddPermission("post-administrator-for-even-ids", "post-edit", PermissionValue.Allow);
			filter.AddPermission("post-administrator-for-even-ids", "post-approve", PermissionValue.Allow);
			
			// apply filter
			string[] titles = filter
				.Filter(posts, permission, users[user].ID)
				.OrderBy(p => p.ID)
				.Select(p => p.Title)
				.ToArray();
			return String.Join(", ", titles);
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Get the users with which to test security rules.</summary>
		protected virtual IDictionary<string, User> GetTestUsers()
		{
			return new Dictionary<string, User> {
				{ "submitter", new User(1, "Submitter of post #1") },
				{ "editor", new User(2, "Editor of post #2") },
				{ "evenAdmin", new User(4, "Administrator of posts with even IDs") }
			};
		}

		/// <summary>Get the sample content with which to test security rules.</summary>
		/// <param name="users">The sample users.</param>
		protected virtual IQueryable<BlogPost> GetTestPosts(IDictionary<string, User> users)
		{
			return new[]
			{
				new BlogPost(1, "The best post", users["submitter"], users["submitter"]),
				new BlogPost(2, "The most ambitious post", users["submitter"], users["editor"]),
				new BlogPost(3, "The forgotten post", null, null),
				new BlogPost(4, "The abandoned post", null, null)
			}.AsQueryable();
		}
	}
}
