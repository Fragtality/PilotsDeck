using System;
using System.Runtime.Serialization;

namespace StreamDeckLib
{
	[Serializable]
	public class ActionNotRegisteredException : Exception
	{
		private static string GenerateErrorMessageForUUID(string uuid) => $"The action with the UUID of \"{uuid}\" was not registered with the ConnectionManager.";

		public string UUID { get; }

		public ActionNotRegisteredException()
		{
		}

		public ActionNotRegisteredException(string uuid) : base(GenerateErrorMessageForUUID(uuid))
		{
			this.UUID = uuid;
		}

		public ActionNotRegisteredException(string uuid, Exception innerException) : base(GenerateErrorMessageForUUID(uuid), innerException)
		{
			this.UUID = uuid;
		}


		protected ActionNotRegisteredException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}


	}

}
