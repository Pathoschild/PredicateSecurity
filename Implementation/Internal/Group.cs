using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Pathoschild.PredicateSecurity.Internal
{
	/// <summary>A named group of security permissions which can be matched to users.</summary>
	public class Group
	{
		/*********
		** Accessors
		*********/
		/// <summary>The name of the group.</summary>
		public string Name { get; set; }

		/// <summary>The type of content matched by this group, if it is relative.</summary>
		public Type ContentType { get; set; }

		/// <summary>A predicate in the form <c>Expression&lt;Func&lt;TContent, int, bool&gt;&gt;</c> that returns true if this group applies for the input content and userID.</summary>
		public object Match { get; set; }

		/// <summary>The permissions contained by this group.</summary>
		public IDictionary<string, PermissionValue> Permissions { get; set; }


		/*********
		** Public methods
		*********/
		/// <summary>Construct an instance.</summary>
		/// <param name="name">The name of the group.</param>
		/// <param name="contentType">The type of content matched by this group, if it is relative.</param>
		/// <param name="match">A predicate in the form <c>Expression&lt;Func&lt;TContent, int, bool&gt;&gt;</c> that returns true if this group applies for the input content and userID.</param>
		public Group(string name, Type contentType, object match)
		{
			this.Name = name;
			this.ContentType = contentType;
			this.Match = match;
			this.Permissions = new Dictionary<string, PermissionValue>(StringComparer.InvariantCultureIgnoreCase);
		}

		/// <summary>Get the predicate that returns true if this group applies for the input content and userID.</summary>
		/// <typeparam name="TContent">The content type to match.</typeparam>
		/// <typeparam name="TUserKey">The type of the key which uniquely identifies the user.</typeparam>
		public Expression<Func<TContent, TUserKey, bool>> GetExpression<TContent, TUserKey>()
		{
			if (!(this.Match is Expression<Func<TContent, TUserKey, bool>>))
				throw new InvalidCastException(String.Format("The security group {0} is not relevant to content of type {1}. It can only be applied to content of type {2}.", this.Name, typeof(TContent), this.ContentType));
			return this.Match as Expression<Func<TContent, TUserKey, bool>>;
		}
	}
}