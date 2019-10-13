using Akka.Actor;
using Akka.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Akka.Cluster.ClusterEvent;

namespace Akka.Cluster.SplitBrainResolver
{
    public sealed class KeepMajorityRefereeDowningStrategy : IDowningStrategy
    {
        public Address Address { get; }
        public string Role { get; }

        public KeepMajorityRefereeDowningStrategy(string address, string role = null)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address));
            Address = Address.Parse(address);
            Role = role;
        }

        public KeepMajorityRefereeDowningStrategy(Config config)
            : this(
                config.GetString("akka.cluster.split-brain-resolver.keep-referee.address"),
                role: config.GetString("akka.cluster.split-brain-resolver.keep-majority.role")
                //config.GetInt("akka.cluster.split-brain-resolver.keep-referee.down-all-if-less-than-nodes")
            )
        {
        }

        public IEnumerable<Member> GetVictims(CurrentClusterState clusterState)
        {
            var members = clusterState.GetMembers(Role);
            var unreachableMembers = clusterState.GetUnreachableMembers(Role);
            var availableMembers = clusterState.GetAvailableMembers(Role);

            var unreachableReferees = unreachableMembers.Where(m => m.Address.Equals(Address));
            var availableReferees = availableMembers.Where(m => m.Address.Equals(Address));

            int unreachableCount = unreachableReferees.Count();
            int availableCount = availableReferees.Count();

            if (availableCount == unreachableCount)
            {
                var oldest = members.Where(m => m.Address.Equals(Address)).SortByAge().FirstOrDefault();
                if (oldest != null && availableReferees.Contains(oldest))
                {
                    return clusterState.GetUnreachableMembers();
                }
                else
                {
                    return clusterState.GetMembers();
                }
            }

            return availableCount < unreachableCount
                ? clusterState.GetMembers()
                : clusterState.GetUnreachableMembers();
        }
    }
}
