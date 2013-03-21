﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Binbin.Linq;
using Pathoschild.PredicateSecurity.Internal;

namespace Pathoschild.PredicateSecurity
{
	/// <summary>Filters collections of arbitrary elements using application-defined security predicates that match users to permission groups.</summary>
	/// <typeparam name="TUserKey">The type of the key which uniquely identifies the user.</typeparam>
	/// <remarks>
	/// This class implements content-relational security. It lets you define LINQ predicates that match users to relational groups (such as "blog-submitter" or "blog-owner"), assign permissions to those groups, and then filter arbitrary collections by specifying a required permission.
	/// 
	/// A minimal example looks like the following. The defines a new group that can edit blogs, and then applies a filter to a database queries so only blogs the current user can edit are returned.
	/// <code>
	/// // which blogs can I edit?
	/// SecurityPredicateFilter filter = new SecurityPredicateFilter();
	/// filter.AddGroup&lt;Blog&gt;("blog-editor", (blog, userID) => blog.Champion.ID == userID));
	/// filter.AddPermission("blog-editor", "blog-edit", PermissionValue.Allow);
	/// 
	/// IQueryable&lt;Blog&gt; canEdit = filter.Filter(db.Get&lt;Blog&gt;(), "blog-edit", user.ID);
	/// </code>
	/// </remarks>
	public class PredicateFilter<TUserKey>
	{
		/*********
		** Accessors
		*********/
		/// <summary>The named groups of security permissions which can be matched to users.</summary>
		public ICollection<Group> Groups { get; set; }


		/*********
		** Public methods
		*********/
		/// <summary>Construct an instance.</summary>
		public PredicateFilter()
		{
			this.Groups = new List<Group>();
		}

		/// <summary>Define a new group of permissions which can be matched to users.</summary>
		/// <typeparam name="TContent">The type of content to which the group can be matched.</typeparam>
		/// <param name="name">The name of the group.</param>
		/// <param name="match">A predicate that returns true if this group applies for the input content and userKey.</param>
		/// <returns>Returns the current instance for chaining.</returns>
		public PredicateFilter<TUserKey> AddGroup<TContent>(string name, Expression<Func<TContent, int, bool>> match)
		{
			this.Groups.Add(new Group(name, typeof(TContent), match));
			return this;
		}

		/// <summary>Define a new permission for a group.</summary>
		/// <param name="groupName">The name of the group to which to add permissions.</param>
		/// <param name="name">The name of the permission to add.</param>
		/// <param name="value">The security behaviour to apply for this group permission.</param>
		/// <returns>Returns the current instance for chaining.</returns>
		public PredicateFilter<TUserKey> AddPermission(string groupName, string name, PermissionValue value)
		{
			// get group
			Group group = this.Groups.FirstOrDefault(p => p.Name == groupName);
			if (group == null)
				throw new InvalidOperationException(String.Format("There is no group named '{0}'.", groupName));

			// add permission
			group.Permissions[name] = value;
			return this;
		}

		/// <summary>Apply a predicate that enforces the configured security rules on a query.</summary>
		/// <typeparam name="TContent">The type of content to predicate.</typeparam>
		/// <param name="content">The content to filter.</param>
		/// <param name="permission">The name of the permission to predicate.</param>
		/// <param name="userKey">The unique key of the user to pass to the group predicate.</param>
		public IQueryable<TContent> Filter<TContent>(IQueryable<TContent> content, string permission, TUserKey userKey)
		{
			var predicate = this.BuildPredicate<TContent>(permission, userKey);
			return content.Where(predicate);
		}

		/// <summary>Construct a predicate that returns true if the user has a permission for a content.</summary>
		/// <typeparam name="TContent">The type of content to predicate.</typeparam>
		/// <param name="permission">The name of the permission to predicate.</param>
		/// <param name="userKey">The unique key of the user to pass to the group predicate.</param>
		Expression<Func<TContent, bool>> BuildPredicate<TContent>(string permission, TUserKey userKey)
		{
			// get expressions
			Group[] groups = this.Groups.Where(p => p.Permissions.ContainsKey(permission)).ToArray();
			Expression<Func<TContent, bool>>[] allowExpressions = this.GetPredicates<TContent>(groups.Where(p => p.Permissions[permission] == PermissionValue.Allow), userKey).ToArray();
			Expression<Func<TContent, bool>>[] denyExpressions = this.GetPredicates<TContent>(groups.Where(p => p.Permissions[permission] == PermissionValue.Deny), userKey, negate: true).ToArray();
			if (!allowExpressions.Any())
				return this.GetConstantValuePredicate<TContent>(false);

			// build predicate
			Expression<Func<TContent, bool>> allowPredicate = this.MergePredicates<TContent>(allowExpressions);
			Expression<Func<TContent, bool>> denyPredicate = this.MergePredicates<TContent>(denyExpressions, BinaryOp.And);
			Expression<Func<TContent, bool>> predicate = allowPredicate.And(denyPredicate);

			return predicate;
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Combine a sequence of predicates into a single predicate.</summary>
		/// <typeparam name="TContent">The type of content to predicate.</typeparam>
		/// <param name="predicates">The predicates to combine.</param>
		/// <param name="operator">The operator with which to combine predicates.</param>
		/// <param name="defaultReturnValue">The default value the predicate should return if the <paramref name="predicates"/> collection is empty.</param>
		protected Expression<Func<TContent, bool>> MergePredicates<TContent>(Expression<Func<TContent, bool>>[] predicates, BinaryOp @operator = BinaryOp.Or, bool defaultReturnValue = true)
		{
			if (!predicates.Any())
				return this.GetConstantValuePredicate<TContent>(defaultReturnValue);

			var predicate = predicates.First();
			return predicates.Skip(1).Aggregate(predicate, (current, expression) => @operator == BinaryOp.Or ? current.Or(expression) : current.And(expression));
		}

		/// <summary>Get a sequence of predicates that return true if the user matches each group.</summary>
		/// <typeparam name="TContent">The type of content to predicate.</typeparam>
		/// <param name="groups">The groups for which to get predicates.</param>
		/// <param name="userKey">The unique key of the user to pass to the group predicates.</param>
		/// <param name="negate">Whether to negate the group predicates, so that they return true if the user does <em>not</em> match each group instead.</param>
		protected IEnumerable<Expression<Func<TContent, bool>>> GetPredicates<TContent>(IEnumerable<Group> groups, TUserKey userKey, bool negate = false)
		{
			return groups
				.Select(group => group.GetExpression<TContent>())
				.Select(predicate => this.GetPredicate(predicate, userKey, negate));
		}

		/// <summary>Construct a predicate that returns true if the user matches a group. This constructs a wrapper around a predicate returned by <see cref="Group.GetExpression{TContent}"/>.</summary>
		/// <typeparam name="TContent">The type of content to predicate.</typeparam>
		/// <param name="predicate">The predicate which returns true if the user matches the group.</param>
		/// <param name="userKey">The unique key of the user to pass to the group predicate.</param>
		/// <param name="negate">Whether to negate the group predicate, so that it returns true if the user does <em>not</em> match the group instead.</param>
		protected Expression<Func<TContent, bool>> GetPredicate<TContent>(Expression<Func<TContent, int, bool>> predicate, TUserKey userKey, bool negate = false)
		{
			return Expression.Lambda<Func<TContent, bool>>(
					Expression.Invoke(
						negate ? this.Negate(predicate) : predicate,
						predicate.Parameters.First(),
						Expression.Constant(userKey, typeof(int))
					),
					predicate.Parameters.First()
				);
		}

		/// <summary>Negate a group predicate, so that it returns the opposite of its normal value.</summary>
		/// <typeparam name="TContent">The type of content to predicate.</typeparam>
		/// <param name="predicate">The predicate to inverse.</param>
		protected Expression<Func<TContent, int, bool>> Negate<TContent>(Expression<Func<TContent, int, bool>> predicate)
		{
			return Expression.Lambda<Func<TContent, int, bool>>(
				Expression.Not(predicate.Body),
				predicate.Parameters
			);
		}

		protected Expression<Func<TContent, bool>> GetConstantValuePredicate<TContent>(bool value)
		{
			return Expression.Lambda<Func<TContent, bool>>(
				Expression.Equal(Expression.Constant(value), Expression.Constant(true)),
				Expression.Parameter(typeof(TContent), "content")
			);
		}
	}
}
