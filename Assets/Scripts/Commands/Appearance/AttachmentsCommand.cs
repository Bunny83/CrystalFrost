using System;
using System.Collections.Generic;

namespace OpenMetaverse.TestClient_
{
    public class AttachmentsCommand : Command
    {
        public AttachmentsCommand(TestClient testClient)
        {
            Client = testClient;
            Name = "attachments";
            Description = "Prints a list of the currently known agent attachments";
            Category = CommandCategory.Appearance;
        }

        public override string Execute(string[] args, UUID fromAgentID)
        {
            List<Primitive> attachments = Client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                prim => prim.ParentID == Client.Self.LocalID
            );

            foreach (var prim in attachments)
            {
                AttachmentPoint point = StateToAttachmentPoint(prim.PrimData.State);

                // TODO: Fetch properties for the objects with missing property sets so we can show names
                Logger.Log($"[Attachment @ {point}] LocalID: {prim.LocalID} UUID: {prim.ID} Offset: {prim.Position}", 
                    Helpers.LogLevel.Info, Client);
            }

            return "Found " + attachments.Count + " attachments";
        }

        public static AttachmentPoint StateToAttachmentPoint(uint state)
        {
            const uint ATTACHMENT_MASK = 0xF0;
            uint fixedState = (((byte)state & ATTACHMENT_MASK) >> 4) | (((byte)state & ~ATTACHMENT_MASK) << 4);
            return (AttachmentPoint)fixedState;
        }
    }
}
