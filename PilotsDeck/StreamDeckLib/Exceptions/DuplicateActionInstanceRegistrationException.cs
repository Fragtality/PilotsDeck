using System;
using System.Runtime.Serialization;

namespace StreamDeckLib
{
  [Serializable]
  public class DuplicateActionInstanceRegistrationException : Exception
  {
    private static string GenerateErrorMessageForUUID(string key) => $"An action with a key value of \"{key}\" has already been registered.";

    public string Key { get; }

    public DuplicateActionInstanceRegistrationException(string key) : base(GenerateErrorMessageForUUID(key))
    {
      this.Key = key;
    }

    public DuplicateActionInstanceRegistrationException(string uuid, Exception innerException) : base(GenerateErrorMessageForUUID(uuid), innerException)
    {
    }

    protected DuplicateActionInstanceRegistrationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }


}
