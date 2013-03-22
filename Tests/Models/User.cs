using System.Collections.Generic;

namespace Pathoschild.PredicateSecurity.Tests.Models
{
	/// <summary>A sample user object.</summary>
	public class User : Entity
	{
		/*********
		** Accessors
		*********/
		/// <summary>A friendly display name.</summary>
		public string Name { get; set; }

		/// <summary>The global permissions assigned to this user.</summary>
		/// <remarks>This is a simplified example of traditional (non-relational) security groups.</remarks>
		public IEnumerable<string> Permissions { get; set; }


		/*********
		** Public methods
		*********/
		/// <summary>Construct an instance.</summary>
		public User() { }

		/// <summary>Construct an instance.</summary>
		/// <param name="id">The unique ID used to match groups in security predicates.</param>
		/// <param name="name">A friendly display name.</param>
		/// <param name="globalPermissions">The global permissions assigned to this user.</param>
		public User(int id, string name, params string[] globalPermissions)
		{
			this.ID = id;
			this.Name = name;
			this.Permissions = globalPermissions;
		}
	}
}