/*
 * Copyright 2023-2024 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// FILE:Runtime/Sources/Transport/RagonLiteNetLibConnection.cs ROLE:Unity UDP transport owner (LiteNetLib client) DEPS:LiteNetLib;Ragon.Client.INetworkConnection;Ragon.Protocol
// API:RagonLiteNetLibConnection() ; Prepare() ; Connect(host,port,protocol) ; Update() ; Disconnect() ; Close() ; Dispose() ; Reliable/Unreliable
// WHY:UnsyncedEvents=false makes every callback fire from Update()->PollEvents() on the Unity main thread, matching the ENet transport contract
// WHY:Connect() writes the protocol as a uint and the v2 server's ConnectionRequest reads GetUInt(); changing this ordering breaks the handshake

using System;
using LiteNetLib;
using LiteNetLib.Utils;
using Ragon.Protocol;

namespace Ragon.Client.Unity
{
  public class RagonLiteNetLibConnection : INetworkConnection
  {
    private const byte ChannelsCount = 2;
    private const byte ReliableChannelId = 0;
    private const byte UnreliableChannelId = 1;

    public ushort Id { get; private set; }
    public NetworkStatistics Statistics { get; private set; }
    public INetworkChannel Reliable { get; private set; }
    public INetworkChannel Unreliable { get; private set; }

    public Action<byte[]> OnData { get; set; }
    public Action OnConnected { get; set; }
    public Action<RagonDisconnect> OnDisconnected { get; set; }

    public ulong BytesSent => (ulong)_manager.Statistics.BytesSent;
    public ulong BytesReceived => (ulong)_manager.Statistics.BytesReceived;
    public int Ping => _peer?.RoundTripTime ?? 0;

    private readonly EventBasedNetListener _listener;
    private readonly NetManager _manager;
    private NetPeer _peer;
    private bool _started;

    public RagonLiteNetLibConnection()
    {
      _listener = new EventBasedNetListener();
      _manager = new NetManager(_listener)
      {
        ChannelsCount = ChannelsCount,
        AutoRecycle = false,
        UnsyncedEvents = false,
        EnableStatistics = true,
      };

      _listener.PeerConnectedEvent += OnPeerConnected;
      _listener.PeerDisconnectedEvent += OnPeerDisconnected;
      _listener.NetworkReceiveEvent += OnNetworkReceive;

      Prepare();
    }

    public void Prepare()
    {
      if (_started)
        return;

      _started = _manager.Start();
      if (!_started)
        RagonLog.Error("Failed to start LiteNetLib client socket");
    }

    public void Connect(string server, ushort port, uint protocol)
    {
      Prepare();

      var writer = new NetDataWriter();
      writer.Put(protocol);

      _peer = _manager.Connect(server, port, writer);
    }

    public void Update()
    {
      if (!_started)
        return;

      _manager.PollEvents();
    }

    public void Disconnect()
    {
      _peer?.Disconnect();
    }

    public void Dispose()
    {
      _peer = null;
      _started = false;

      _manager.Stop();
    }

    public void Close()
    {
      Disconnect();
    }

    private void OnPeerConnected(NetPeer peer)
    {
      _peer = peer;
      Id = (ushort)peer.Id;
      Statistics = new NetworkStatistics();
      Reliable = new RagonLiteNetLibReliableChannel(peer);
      Unreliable = new RagonLiteNetLibUnreliableChannel(peer);

      OnConnected?.Invoke();
    }

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
      _peer = null;

      var reason = disconnectInfo.Reason == DisconnectReason.Timeout
        ? RagonDisconnect.TIMEOUT
        : RagonDisconnect.SERVER;

      OnDisconnected?.Invoke(reason);
    }

    private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
      var data = reader.GetRemainingBytes();
      reader.Recycle();

      OnData?.Invoke(data);
    }
  }
}
