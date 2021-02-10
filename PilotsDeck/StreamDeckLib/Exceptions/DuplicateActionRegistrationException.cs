using System;
using System.Runtime.Serialization;

namespace StreamDeckLib
{
  [Serializable]
  public class DuplicateActionRegistrationException : Exception
  {
    private static string GenerateErrorMessageForUUID(string uuid) => $"An action with the UUID of \"{uuid}\" has already been registered with the ConnectionManager.";

    public string UUID { get; }

    public DuplicateActionRegistrationException(string uuid) : base(GenerateErrorMessageForUUID(uuid))
    {
      this.UUID = uuid;
    }

    public DuplicateActionRegistrationException(string uuid, Exception innerException) : base(GenerateErrorMessageForUUID(uuid), innerException)
    {
    }

    protected DuplicateActionRegistrationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }

}
