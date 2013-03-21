namespace Pathoschild.PredicateSecurity.Tests.Models
{
	/// <summary>A sample content object.</summary>
	public class BlogPost : Entity
	{
		/*********
		** Accessors
		*********/
		/// <summary>A unique display title.</summary>
		public string Title { get; set; }

		/// <summary>The user who submitted the blog post.</summary>
		public User Submitter { get; set; }

		/// <summary>The user who is the current idea editor.</summary>
		public User Editor { get; set; }


		/*********
		** Public methods
		*********/
		/// <summary>Construct an instance.</summary>
		public BlogPost() { }

		/// <summary>Construct an instance.</summary>
		/// <param name="id">The unique ID which identifies this entity.</param>
		/// <param name="title">A unique display title.</param>
		/// <param name="submitter">The user who submitted the blog post.</param>
		/// <param name="editor">The user who is the current idea editor.</param>
		public BlogPost(int id, string title, User submitter, User editor)
		{
			this.ID = id;
			this.Title = title;
			this.Submitter = submitter;
			this.Editor = editor;
		}
	}
}