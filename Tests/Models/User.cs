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


		/*********
		** Public methods
		*********/
		/// <summary>Construct an instance.</summary>
		public User() { }

		/// <summary>Construct an instance.</summary>
		/// <param name="id">The unique ID used to match groups in security predicates.</param>
		/// <param name="name">A friendly display name.</param>
		public User(int id, string name)
		{
			this.ID = id;
			this.Name = name;
		}
	}
}