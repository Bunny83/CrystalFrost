using System;

namespace OpenMetaverse.TestClient_
{
    public class WhisperCommand : Command
    {
        public WhisperCommand(TestClient testClient)
        {
            Name = "whisper";
            Description = "Whisper something.";
            Category = CommandCategory.Communication;
        }

        public override string Execute(string[] args, UUID fromAgentID)
        {
            int channel = 0;
            int startIndex = 0;
            string message = string.Empty;
            if (args.Length < 1)
            {
                return "usage: whisper (optional channel) whatever";
            }
            else if (args.Length > 1)
            {
                try
                {
                    channel = Convert.ToInt32(args[0]);
                    startIndex = 1;
                }
                catch (FormatException)
                {
                    channel = 0;
                }
            }

            for (int i = startIndex; i < args.Length; i++)
            {
                // Append a space before the next arg
                if( i > 0 )
                    message += " ";
                message += args[i];
            }

            Client.Self.Chat(message, channel, ChatType.Whisper);

            return "Whispered " + message;
        }
    }
}