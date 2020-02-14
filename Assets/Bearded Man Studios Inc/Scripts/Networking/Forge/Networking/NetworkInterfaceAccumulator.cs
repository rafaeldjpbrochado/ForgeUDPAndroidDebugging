using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace BeardedManStudios.Forge.Networking
{
    public class NetworkInterfaceAccumulator
    {
        public List<IPAddress> IPs { get; private set; }

        public NetworkInterfaceAccumulator()
        {
            IPs = new List<IPAddress>();
        }

        public void Accumulate(AddressFamily family)
        {
            IPs.Clear();
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                AddNetworkInterfaceIfPossible(nic, family);
            }
        }

        private void AddIfInFamily(NetworkInterface nic, AddressFamily family)
        {
            foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily == family)
                {
                    IPs.Add(ip.Address);
                }
            }
        }

        private bool checkForValidAndroidNIC(NetworkInterface nic)
        {
            switch (nic.Name)
            {
                case "wlan0": // Wifi
                    break;
                default:
                    return false;
            }

            switch (nic.OperationalStatus)
            {
                case OperationalStatus.Up:
                case OperationalStatus.Testing:
                case OperationalStatus.Unknown:
                case OperationalStatus.Dormant:
                    break;
                case OperationalStatus.Down:
                case OperationalStatus.NotPresent:
                case OperationalStatus.LowerLayerDown:
                default:
                    return false;
            }

            return true;
        }

        private bool checkForValidNIC(NetworkInterface nic)
        {
            switch (nic.NetworkInterfaceType)
            {
                case NetworkInterfaceType.Wireless80211:
                case NetworkInterfaceType.Ethernet:
                    break;
                default:
                    return false;
            }

            if (nic.OperationalStatus != OperationalStatus.Up)
                return false;

            return true;
        }

        private void AddNetworkInterfaceIfPossible(NetworkInterface nic, AddressFamily family)
        {
            if (checkForValidAndroidNIC(nic) || checkForValidNIC(nic))
            {
                AddIfInFamily(nic, family);
            }

        }
    }
}