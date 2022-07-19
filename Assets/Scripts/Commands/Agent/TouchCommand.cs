namespace OpenMetaverse.TestClient_
{
    public class TouchCommand: Command
    {
        public TouchCommand(TestClient testClient)
		{
			Name = "touch";
			Description = "Attempt to touch a prim with specified UUID";
            Category = CommandCategory.Objects;
		}
		
        public override string Execute(string[] args, UUID fromAgentID)
		{
            UUID target;

            if (args.Length != 1)
                return "Usage: touch UUID";
            
            if (UUID.TryParse(args[0], out target))
            {
                Primitive targetPrim = Client.Network.CurrentSim.ObjectsPrimitives.Find(
                    prim => prim.ID == target
                );

                if (targetPrim != null)
                {
                    Client.Self.Touch(targetPrim.LocalID);
                    return "Touched prim " + targetPrim.LocalID;
                }
            }

            return "Couldn't find a prim to touch with UUID " + args[0];
		}
    }
}
