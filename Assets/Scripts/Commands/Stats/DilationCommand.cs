namespace OpenMetaverse.TestClient_
{
    public class DilationCommand : Command
    {
		public DilationCommand(TestClient testClient)
        {
            Name = "dilation";
            Description = "Shows time dilation for current sim.";
            Category = CommandCategory.Simulator;
        }

        public override string Execute(string[] args, UUID fromAgentID)
        {
            return "Dilation is " + Client.Network.CurrentSim.Stats.Dilation;
        }
    }
}