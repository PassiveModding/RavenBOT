using System;
using System.Collections.Generic;
using ahd.Graphite;

namespace RavenBOT.Common
{
    public class GraphiteService
    {
        private GraphiteClient Client { get; }
        private bool Enabled { get; }

        public GraphiteService(string clientUrl = null)
        {
            if (clientUrl == null)
            {
                Enabled = false;
            }
            else
            {
                Client = new GraphiteClient(clientUrl);
                Enabled = true;
            }
        }

        //Exposing the send methods for the graphite client
        public void Report(Datapoint[] datapoints)
        {
            if (!Enabled) return;
            try
            {
                Client.Send(datapoints);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Report(List<Datapoint> datapoints)
        {
            if (!Enabled) return;
            try
            {
                Client.Send(datapoints);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Report(Datapoint point)
        {
            if (!Enabled) return;
            try
            {
                Client.Send(point);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Report(string path, double value, DateTime time)
        {
            if (!Enabled) return;
            try
            {
                Client.Send(path, value, time);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Report(string path, double value)
        {
            if (!Enabled) return;
            try
            {
                Client.Send(path, value);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}