using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pathoschild.PredicateSecurity.Tests.Models;

namespace Pathoschild.PredicateSecurity.Tests
{
	/// <summary>Provides generic unit tests that assert the predicate filter. These tests are agnostic of the underlying data source.</summary>
	/// <remarks>
	/// The following assumptions are tested:
	/// * A submitter can edit any blog for which he is the submitter. He cannot approve his own blogs (even if he is an editor). This asserts the basic Allow scenario.
	/// * An editor can edit any blog for which he is the editor, and can also approve them when he isn't also the submitter. This asserts that Deny takes precedence over Allow.
	/// * A site administrator can edit or approve any blog on the site because he has non-relational permissions (except for those where he is the submitter). This asserts global permissions.
	/// </remarks>
	public abstract class AbstractTests
	{
		/*********
		** Unit tests
		*********/
		/// <summary>Assert that the predicate filter correctly filters a collection based on a set of security rules.</summary>
		/// <param name="user">The name of the user defined by <see cref="GetUsers"/> whose security to predicate.</param>
		/// <param name="permission">The permission to predicate.</param>
		/// <returns>Returns the titles of the blog posts to which the user has the permission.</returns>
		/// <remarks>Each test case asserts security assumptions using the configuration set by <see cref="GetFilter"/>.</remarks>
		[Test(Description = "Assert that the predicate filter correctly filters a collection based on a set of security rules.")]
		
		// John Submitter cannot approve any ideas because he is not the editor for any of them.
		[TestCase("submitter", "post-approve", Result = "")]
		
		// John Submitter can edit his own ideas.
		[TestCase("submitter", "post-edit", Result = "The best post, The most ambitious post")]
		
		// Jane Editor can approve the idea she is an editor for.
		[TestCase("editor", "post-approve", Result = "The most ambitious post")]

		// Jane Editor can edit the idea she is an editor for. (She hasn't submitted any ideas herself.)
		[TestCase("editor", "post-edit", Result = "The most ambitious post")]

		// Jesse SiteAdmin can approve any ideas in the system because of his global permissions, except for the idea he's the submitter for (since the submitter group has a Deny-permission).
		[TestCase("siteAdmin", "post-approve", Result = "The best post, The most ambitious post, The abandoned post")]

		// Jesse SiteAdmin can edit any ideas in the system because of his global permissions.
		[TestCase("siteAdmin", "post-edit", Result = "The best post, The most ambitious post, The forgotten post, The abandoned post")]
		public string Filter_CorrectlyFilters(string user, string permission)
		{
			// set up
			PredicateFilter<User, int> filter = this.GetFilter();
			IDictionary<string, User> users = this.GetUsers();
			IQueryable<BlogPost> posts = this.GetPosts(users);

			// apply filter
			string[] titles = filter
				.Filter(posts, permission, users[user])
				.OrderBy(p => p.ID)
				.Select(p => p.Title)
				.ToArray();
			return String.Join(", ", titles);
		}

		/// <summary>Assert that the predicate filter's convenience <see cref="PredicateFilter{TUser,TUserKey}.Test{TContent}"/> method correctly matches.</summary>
		/// <param name="user">The name of the user defined by <see cref="GetUsers"/> whose security to predicate.</param>
		/// <param name="permission">The permission to predicate.</param>
		/// <returns>Returns whether the user has the permission for the given content.</returns>
		/// <remarks>See the remarks on <see cref="Filter_CorrectlyFilters"/>.</remarks>
		[Test(Description = "Assert that the predicate filter correctly filters a collection based on a set of security rules.")]
		[TestCase("submitter", "post-approve", Result = "")]
		[TestCase("submitter", "post-edit", Result = "The best post, The most ambitious post")]
		[TestCase("editor", "post-approve", Result = "The most ambitious post")]
		[TestCase("editor", "post-edit", Result = "The most ambitious post")]
		[TestCase("siteAdmin", "post-approve", Result = "The best post, The most ambitious post, The abandoned post")]
		[TestCase("siteAdmin", "post-edit", Result = "The best post, The most ambitious post, The forgotten post, The abandoned post")]
		public string Filter_CorrectlyTests(string user, string permission)
		{
			// set up
			PredicateFilter<User, int> filter = this.GetFilter();
			IDictionary<string, User> users = this.GetUsers();
			BlogPost[] posts = this.GetPosts(users).ToArray();

			// apply filter
			string[] titles = posts
				.Where(p => filter.Test(p, permission, users[user]))
				.OrderBy(p => p.ID)
				.Select(p => p.Title)
				.ToArray();
			return String.Join(", ", titles);
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Get a predicate filter configured with a set of users and permissions that cover the range of security scenarios.</summary>
		protected virtual PredicateFilter<User, int> GetFilter()
		{
			// set up security groups
			PredicateFilter<User, int> filter = new PredicateFilter<User, int>(u => u.ID, u => u.Permissions.ToDictionary(p => p, p => PermissionValue.Allow));
			filter.AddGroup<BlogPost>("post-submitter", (post, userID) => post.Submitter != null && post.Submitter.ID == userID);
			filter.AddGroup<BlogPost>("post-editor", (post, userID) => post.Editor != null && post.Editor.ID == userID);

			// set up permission rules:
			// submitters can edit their own posts, but are never allowed to approve them
			// editors can edit and approve posts they're the editors for (as long as they're not submitters)
			// and post-administrators-for-even-ids can edit and approve any post with an even ID (as long as they're not the submitter)
			filter.AddPermission("post-submitter", "post-edit", PermissionValue.Allow);
			filter.AddPermission("post-submitter", "post-approve", PermissionValue.Deny);
			filter.AddPermission("post-editor", "post-edit", PermissionValue.Allow);
			filter.AddPermission("post-editor", "post-approve", PermissionValue.Allow);

			return filter;
		}

		/// <summary>Get the users with which to test security rules.</summary>
		protected virtual IDictionary<string, User> GetUsers()
		{
			return new Dictionary<string, User> {
				{ "submitter", new User(1, "John (submitter and editor of #1; submitter of #2)") },
				{ "editor", new User(2, "Jane (editor of #2)") },
				{ "siteAdmin", new User(3, "Jesse (site administrator with global permissions; submitter of #3)", "post-edit", "post-approve") }
			};
		}

		/// <summary>Get the sample content with which to test security rules.</summary>
		/// <param name="users">The sample users.</param>
		protected virtual IQueryable<BlogPost> GetPosts(IDictionary<string, User> users)
		{
			return new[]
			{
				new BlogPost(1, "The best post", users["submitter"], users["submitter"]),
				new BlogPost(2, "The most ambitious post", users["submitter"], users["editor"]),
				new BlogPost(3, "The forgotten post", users["siteAdmin"], null),
				new BlogPost(4, "The abandoned post", null, null)
			}.AsQueryable();
		}
	}
}
