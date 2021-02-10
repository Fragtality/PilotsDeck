using System;
using System.Runtime.Serialization;

namespace StreamDeckLib
{
  [Serializable]
  public class IncompleteActionDefinitionException : Exception
  {
    public IncompleteActionDefinitionException(string message) : base(message)
    {
    }

    public IncompleteActionDefinitionException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected IncompleteActionDefinitionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

  }

}
