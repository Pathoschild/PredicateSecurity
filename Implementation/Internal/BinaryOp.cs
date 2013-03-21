namespace Pathoschild.PredicateSecurity.Internal
{
	/// <summary>A binary expression operator.</summary>
	public enum BinaryOp
	{
		/// <summary>Returns true if either expression is true.</summary>
		Or,

		/// <summary>Returns true if both expressions are true.</summary>
		And
	};
}