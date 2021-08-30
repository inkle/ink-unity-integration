#pragma warning disable IDE1006

namespace Ink.Runtime
{
	public interface INamedContent
	{
		string name { get; }
		bool hasValidName { get; }
	}
}

