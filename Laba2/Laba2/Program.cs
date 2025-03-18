using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Threading;

class Laba2
{
    static void Main(string[] args)
    {
        args = new string[1];
        Console.Write("Введите IP-адрес или доменное имя: ");
        args[0] = Console.ReadLine();

        if (args.Length < 1)
        {
            Console.WriteLine("Usage: mytraceroute <hostname/IP> [/r]");
            return;
        }

        string targetHost = args[0];
        bool resolveNames = args.Length > 1 && args[1] == "/r";

        if (!IPAddress.TryParse(targetHost, out IPAddress targetIP))
        {
            try
            {
                targetIP = Dns.GetHostAddresses(targetHost)[0];
            }
            catch
            {
                Console.WriteLine("Неверное имя или IP-адрес");
                return;
            }
        }

        if (targetHost == targetIP.ToString())
        {
            Console.WriteLine($"Трассировка маршрута к {targetHost} с максимальным числом хопов 30:");
        }
        else
        {
            Console.WriteLine($"Трассировка маршрута к {targetHost} [{targetIP}] с максимальным числом хопов 30:");
        }


        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp))
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);
            byte[] receiveBuffer = new byte[1024];

            for (int ttl = 1; ttl <= 30; ttl++)
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, ttl);
                string host = "*";
                string resolvedHost = "";
                int[] times = new int[3];

                for (int i = 0; i < 3; i++)
                {
                    byte[] icmpPacket = CreateIcmpPacket((ushort)(ttl * 3 + i));
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    socket.SendTo(icmpPacket, new IPEndPoint(targetIP, 0));

                    try
                    {
                        int received = socket.ReceiveFrom(receiveBuffer, ref remoteEndPoint);
                        stopwatch.Stop();
                        string currentHost = ((IPEndPoint)remoteEndPoint).Address.ToString();



                        if (resolveNames)
                        {
                            try
                            {
                                resolvedHost = Dns.GetHostEntry(currentHost).HostName;
                            }
                            catch
                            {
                                resolvedHost = currentHost;
                            }
                        }
                        if (host == "*") host = currentHost;
                        times[i] = (int)stopwatch.ElapsedMilliseconds;
                    }
                    catch (SocketException)
                    {
                        times[i] = -1;
                    }
                }
                Thread.Sleep(1000);

                if (FormatTime(times[0]) == "*" && FormatTime(times[1]) == "*" && FormatTime(times[2]) == "*")
                {
                    Console.WriteLine($"{ttl,2}     {FormatTime(times[0])}       {FormatTime(times[1])}       {FormatTime(times[2])}   Превышен интервал ожидания для запроса.");
                }
                else
                {
                    Console.WriteLine($"{ttl,2}   {FormatTime(times[0])}   {FormatTime(times[1])}   {FormatTime(times[2])}   {host} {resolvedHost}");
                }


                if (host != "*" && IPAddress.TryParse(host, out IPAddress foundIP) && foundIP.Equals(targetIP))
                {
                    Console.WriteLine("Трассировка завершена.");
                    break;
                }
            }
        }
    }

    static byte[] CreateIcmpPacket(ushort sequenceNumber)
    {
        byte[] packet = new byte[8];
        packet[0] = 8;
        packet[1] = 0;
        packet[2] = 0;
        packet[3] = 0;
        packet[4] = 0;
        packet[5] = 0;
        packet[6] = (byte)(sequenceNumber >> 8);
        packet[7] = (byte)(sequenceNumber & 0xFF);

        ushort checksum = ComputeChecksum(packet);
        packet[2] = (byte)(checksum >> 8);
        packet[3] = (byte)(checksum & 0xFF);

        return packet;
    }

    static ushort ComputeChecksum(byte[] data)
    {
        int sum = 0;
        for (int i = 0; i < data.Length; i += 2)
        {
            sum += (data[i] << 8) + (i + 1 < data.Length ? data[i + 1] : 0);
        }

        while ((sum >> 16) != 0)
        {
            sum = (sum & 0xFFFF) + (sum >> 16);
        }

        return (ushort)~sum;
    }

    static string FormatTime(int time)
    {
        return time == -1 ? "*" : time + " ms";
    }
}
