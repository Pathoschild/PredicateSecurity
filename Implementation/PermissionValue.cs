namespace Pathoschild.PredicateSecurity
{
	/// <summary>The security behaviour to apply for a group permission.</summary>
	public enum PermissionValue
	{
		/// <summary>This group has no effect on the permission.</summary>
		Inherit,

		/// <summary>This group enables the permission unles it is superceded by a <see cref="Deny"/> value.</summary>
		Allow,

		/// <summary>The group prohibits the permission. This overrides any other value.</summary>
		Deny
	};
}
