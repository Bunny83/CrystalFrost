using System;
using System.Collections.Generic;
using System.Threading;


namespace OpenMetaverse.TestClient_
{
    public class FindObjectsCommand : Command
    {
        Dictionary<UUID, Primitive> PrimsWaiting = new Dictionary<UUID, Primitive>();
        AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

        public FindObjectsCommand(TestClient testClient)
        {
            testClient.Objects.ObjectProperties += Objects_OnObjectProperties;

            Name = "findobjects";
            Description = "Finds all objects, which name contains search-string. " +
                "Usage: findobjects [radius] <search-string>";
            Category = CommandCategory.Objects;
        }

        public override string Execute(string[] args, UUID fromAgentID)
        {
            // *** parse arguments ***
            if ((args.Length < 1) || (args.Length > 2))
                return "Usage: findobjects [radius] <search-string>";
            float radius = float.Parse(args[0]);
            string searchString = (args.Length > 1) ? args[1] : string.Empty;

            // *** get current location ***
            OMVVector3 location = Client.Self.SimPosition;

            // *** find all objects in radius ***
            List<Primitive> prims = Client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                delegate(Primitive prim)
                {
                    OMVVector3 pos = prim.Position;
                    return ((prim.ParentID == 0) && (pos != OMVVector3.Zero) && (OMVVector3.Distance(pos, location) < radius));
                }
            );

            // *** request properties of these objects ***
            bool complete = RequestObjectProperties(prims, 250);

            foreach (Primitive p in prims)
            {
                string name = p.Properties?.Name;
                if (string.IsNullOrEmpty(searchString) || ((name != null) && (name.Contains(searchString))))
                    Jenny.Console.WriteLine(String.Format("Object '{0}': {1}", name, p.ID.ToString()));
            }

            if (complete) return "Done searching";
            Jenny.Console.WriteLine("Warning: Unable to retrieve full properties for:");
            foreach (UUID uuid in PrimsWaiting.Keys)
                Jenny.Console.WriteLine(uuid.ToString());

            return "Done searching";
        }

        private bool RequestObjectProperties(List<Primitive> objects, int msPerRequest)
        {
            // Create an array of the local IDs of all the prims we are requesting properties for
            uint[] localids = new uint[objects.Count];

            lock (PrimsWaiting)
            {
                PrimsWaiting.Clear();

                for (int i = 0; i < objects.Count; ++i)
                {
                    localids[i] = objects[i].LocalID;
                    PrimsWaiting.Add(objects[i].ID, objects[i]);
                }
            }

            Client.Objects.SelectObjects(Client.Network.CurrentSim, localids);

            return AllPropertiesReceived.WaitOne(2000 + msPerRequest * objects.Count, false);
        }

        void Objects_OnObjectProperties(object sender, ObjectPropertiesEventArgs e)
        {
            lock (PrimsWaiting)
            {
                Primitive prim;
                if (PrimsWaiting.TryGetValue(e.Properties.ObjectID, out prim))
                {
                    prim.Properties = e.Properties;
                }
                PrimsWaiting.Remove(e.Properties.ObjectID);

                if (PrimsWaiting.Count == 0)
                    AllPropertiesReceived.Set();
            }
        }
    }
}
