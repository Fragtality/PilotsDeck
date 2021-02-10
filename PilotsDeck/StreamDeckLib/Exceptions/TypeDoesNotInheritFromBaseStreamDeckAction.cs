using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace StreamDeckLib
{
  [Serializable]
  public class TypeDoesNotInheritFromBaseStreamDeckAction : Exception
  {
    private string fullName;
    private string  assemblyName;

    public TypeDoesNotInheritFromBaseStreamDeckAction()
    {
    }

    public TypeDoesNotInheritFromBaseStreamDeckAction(string message) : base(message)
    {
    }


		public TypeDoesNotInheritFromBaseStreamDeckAction(string simpleName, string fullName, string assemblyName)
		: this($"The type \"{simpleName}\" (\"{fullName}\") from assembly \"{assemblyName}\" does not inherit from required base class \"{nameof(BaseStreamDeckAction)}\".")
    {

			this.fullName = fullName;

			this.assemblyName = assemblyName;

		}


	public TypeDoesNotInheritFromBaseStreamDeckAction(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected TypeDoesNotInheritFromBaseStreamDeckAction(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}