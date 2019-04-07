using System;
using System.Collections.Generic;
using ahd.Graphite;

namespace RavenBOT.Services
{
    public class GraphiteService
    {
        private GraphiteClient Client { get; }
        private bool enabled { get; }

        public GraphiteService(GraphiteClient client)
        {
            enabled = true;
            if (client == null)
            {
                enabled = false;
            }
            Client = client;
        }

        //Exposing the send methods for the graphite client

        public void Report(Datapoint[] datapoints)
        {
            if (!enabled) return;
            Client.Send(datapoints);
        }

        public void Report(List<Datapoint> datapoints)
        {
            if (!enabled) return;
            Client.Send(datapoints);
        }

        public void Report(Datapoint point)
        {
            if (!enabled) return;
            Client.Send(point);
        }

        public void Report(string path, double value, DateTime time)
        {
            if (!enabled) return;
            Client.Send(path, value, time);
        }

        public void Report(string path, double value)
        {
            if (!enabled) return;
            Client.Send(path, value);
        }
    }
}
