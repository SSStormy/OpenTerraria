﻿using System;
using System.Linq;
using System.Threading.Tasks;
using OpenTerrariaClient.Model;
using OpenTerrariaClient.Model.ID;
using OpenTerrariaClient.Packet;

namespace OpenTerrariaClient.Client.Service
{
    ///<summary>Handles the parsing and handling of packets to which we must reply with an action.</summary>
    internal class InternalPacketManagerService : IService
    {
        private PacketEventService _events;
        private TerrariaClient _client;

        public void Install(TerrariaClient client)
        {
            _client = client;
            _events = client.Packets();

            if (_events == null)
                throw new NullReferenceException(
                    $"{nameof(client)} doesn't have a valid {typeof (PacketEventService).Name} value.");

            #region Critical

            _events.Subscribe(TerrPacketType.Disconnect, packet =>
            {
                using (PayloadReader reader = new PayloadReader(packet.Payload))
                    client.SetDisconnectState(reader.ReadString());
            });

            _events.Subscribe(TerrPacketType.ContinueConnecting, packet =>
            {
                if (!client.IsLoggingIn) return;

                client.OnLoggedIn(packet.Payload[0]);
                SendLoginPackets();
            });

            _events.Subscribe(TerrPacketType.RequestPassword, packet =>
            {
                if (string.IsNullOrEmpty(client.Config.Password))
                    throw new ArgumentNullException(client.Config.Password);

                client.Send(TerrPacketType.SendPassword, client.Config.Password);
            });

            #endregion

            #region Player

            _events.Subscribe(TerrPacketType.TogglePvp, packet =>
            {
                TogglePvp pvpState = PacketWrapper.Parse<TogglePvp>(packet);
                Player player = _client.GetPlayer(pvpState.PlayerId);
                player.IsPvp = pvpState.Value;
            });

            _events.Subscribe(TerrPacketType.PlayerAppearance, packet =>
            {
                PlayerAppearance appearance = PacketWrapper.Parse<PlayerAppearance>(packet);
                Player player = _client.GetPlayer(appearance.PlayerId.Value);

                player.Appearance = appearance;
            });

            _events.Subscribe(TerrPacketType.SetInventory, packet =>
            {

                GameItem setItem = PacketWrapper.Parse<GameItem>(packet);
                Player player = _client.GetPlayer(setItem.PlayerId.Value);

                if (player.Inventory == null)
                    player.Inventory = new PlayerInventory();

                player.Inventory.InternalItems[setItem.SlotId.Value] = setItem;
            });

            _events.Subscribe(TerrPacketType.PlayerLife, packet =>
            {
                ValPidPair<short> lifePair = PacketWrapper.Parse<ValPidPair<short>>(packet);
                Player player = _client.GetPlayer(lifePair.PlayerId.Value);

                player.Health = lifePair;
            });

            _events.Subscribe(TerrPacketType.PlayerMana, packet =>
            {
                ValPidPair<short> manaPair = PacketWrapper.Parse<ValPidPair<short>>(packet);
                Player player = _client.GetPlayer(manaPair.PlayerId.Value);

                player.Mana = manaPair;
            });

            _events.Subscribe(TerrPacketType.UpdatePlayerBuff, packet =>
            {
                BuffList buffs = PacketWrapper.Parse<BuffList>(packet);
                Player player = _client.GetPlayer(buffs.PlayerId.Value);

                player.Buffs = buffs;
            });

            _events.Subscribe(TerrPacketType.AddPlayerBuff, packet =>
            {
                AddPlayerBuff addPlayerBuff = PacketWrapper.Parse<AddPlayerBuff>(packet);
                _client.RegisterPlayer(addPlayerBuff.PlayerId);
                _client.Log.Info(
                    $"Add player buff pid {addPlayerBuff.PlayerId} buff: {addPlayerBuff.Buff} time: {addPlayerBuff.Time}");
            });

            _events.Subscribe(TerrPacketType.UpdatePlayer, packet =>
            {
                UpdatePlayer update = PacketWrapper.Parse<UpdatePlayer>(packet);
                Player player = _client.GetPlayer(update.PlayerId);
                player.Position = update.Position;
                player.Velocity = update.Velocity;
                player.SelectedItem = update.SelectedItem;
                player.Control = update.Control;
                player.Pulley = update.Pulley;
            });

            _events.Subscribe(TerrPacketType.PlayerActive, packet =>
            {
                PlayerActive active = PacketWrapper.Parse<PlayerActive>(packet);
                if (active.Active)
                    _client.RegisterPlayer(active.PlayerId);
                else
                    client.RemovePlayer(active.PlayerId);
            });

            _events.Subscribe(TerrPacketType.PlayerTeam, packet =>
            {
                PlayerTeam team = PacketWrapper.Parse<PlayerTeam>(packet);
                _client.GetPlayer(team.PlayerId).Team = team.Team;
            });

            #endregion

            #region World

            _events.Subscribe(TerrPacketType.WorldInformation,
                packet => _client.World = PacketWrapper.Parse<WorldInfo>(packet));

            _events.Subscribe(TerrPacketType.Time, packet =>
            {
                WorldTime time = PacketWrapper.Parse<WorldTime>(packet);
                _client.World.Time = time.Time;
                _client.World.IsDay = time.IsDay;
                _client.World.SunModY = time.SunModY;
                _client.World.MoonModY = time.MoonModY;
            });

            _events.Subscribe(TerrPacketType.NotifyPlayerOfEvent, packet =>
                _client.OnWorldEventBegin(BitConverter.ToInt16(packet.Payload, 0)));

            _events.Subscribe(TerrPacketType.SetNpcShopItem, packet =>
            {
                _client.Log.Info("Received shop item data.");
            });

            #endregion

            #region Npc

            if (_client.Config.TrackNpcData)
            {
                _events.Subscribe(TerrPacketType.NpcUpdate, packet =>
                {
                    Npc npc = PacketWrapper.Parse<Npc>(packet);
                    _client.NpcAddOrUpdate(npc);
                });
                _events.Subscribe(TerrPacketType.UpdateNpcName, packet =>
                {
                    UpdateNpcName npcName = PacketWrapper.Parse<UpdateNpcName>(packet);
                    _client.GetExistingNpc(npcName.UniqueNpcId).Name = npcName.Name;
                });
                _events.Subscribe(TerrPacketType.TravellingMerchantInventory, packet =>
                {
                    TravellingMerchantInventory travellingMerchant =
                        PacketWrapper.Parse<TravellingMerchantInventory>(packet);

                    // find the traveling merchant in the npc list
                    foreach (var pair in client._npcs.Where(pair => pair.Value.NpcId == NpcId.TravellingMerchant))
                        pair.Value.Shop = travellingMerchant.Items;
                });
                _events.Subscribe(TerrPacketType.SetNpcKillCount,
                    packet => _client.World.SetNpcKc(PacketWrapper.Parse<SetNpcKillCount>(packet)));

                _events.Subscribe(TerrPacketType.NpcHomeUpdate, packet =>
                {
                    NpcHomeUpdate homeUpdate = PacketWrapper.Parse<NpcHomeUpdate>(packet);
                    Npc npc = _client.GetExistingNpc(homeUpdate.UniqueNpcId);
                    npc.HomeTileX = homeUpdate.HomeTileX;
                    npc.HomeTileY = homeUpdate.HomeTileY;
                    npc.IsHomeless = homeUpdate.IsHomeless;
                });

                _events.Subscribe(TerrPacketType.NotifyPlayerNpcKilled, packet =>
                    _client.RemoveNpc(BitConverter.ToInt16(packet.Payload, 0)));
            }

            #endregion

            #region Item

            _events.Subscribe(TerrPacketType.RemoveItemOwner, packet =>
            {
                RemoveItemOwner remItemOwner = PacketWrapper.Parse<RemoveItemOwner>(packet);
                _client.UpdateItemOwner(remItemOwner.ItemIndex, byte.MaxValue);
                // todo : figure out whether a RemoveItemOwner packet should call RemoveItem();
            });

            _events.Subscribe(TerrPacketType.UpdateItemOwner, packet =>
            {
                UpdateItemOwner updateOwner = PacketWrapper.Parse<UpdateItemOwner>(packet);
                _client.UpdateItemOwner(updateOwner.ItemId, updateOwner.Owner);
            });

            if (_client.Config.TrackItemData)
            {
                _events.SubscribeMany(packet =>
                {
                    WorldItem itemDrop = PacketWrapper.Parse<WorldItem>(packet);

                    if (itemDrop.Item.Id == ItemId.None) return;

                    _client.ItemAddOrUpdate(itemDrop);

                }, TerrPacketType.UpdateItemDrop, TerrPacketType.UpdateItemDrop2);
            }
            #endregion

            #region Projectile

            if (_client.Config.TrackProjectileData)
            {
                _events.Subscribe(TerrPacketType.ProjectileUpdate, packet =>
                {
                    WorldProjectile projUpdate = PacketWrapper.Parse<WorldProjectile>(packet);
                    _client.ProjectileAddOrUpdate(projUpdate);
                });

                _events.Subscribe(TerrPacketType.DestroyProjectile, packet =>
                {
                    DestroyProjectile destroyProjectile = PacketWrapper.Parse<DestroyProjectile>(packet);
                    _client.RemoveProjectile(destroyProjectile.ProjectileId);
                });
            }

            #endregion

            #region Misc

            _events.Subscribe(TerrPacketType.Statusbar,
                packet => _client.OnStatusReceived(PacketWrapper.Parse<Status>(packet)));

            _events.Subscribe(TerrPacketType.ChatMessage, packet =>
            {
                ChatMessage msg = PacketWrapper.Parse<ChatMessage>(packet);

                client.OnMessageReceived(msg,
                    client.GetPlayer(msg.PlayerId).IsServer
                        ? MessageReceivedEventArgs.SenderType.Server
                        : MessageReceivedEventArgs.SenderType.Player);
            });

            #endregion
        }

        private void SendLoginPackets()
        {
            _client.Send(TerrPacketType.PlayerAppearance, _client.CurrentPlayer.Appearance.CreatePayload());

            if (_client.CurrentPlayer.Guid != null)
                _client.Send(TerrPacketType.ClientUuid, _client.CurrentPlayer.Guid.ToString());

            if (_client.CurrentPlayer.Health != null)
                _client.Send(TerrPacketType.PlayerLife, _client.CurrentPlayer.Health.CreatePayload());

            if (_client.CurrentPlayer.Mana != null)
                _client.Send(TerrPacketType.PlayerMana, _client.CurrentPlayer.Mana.CreatePayload());

            if (_client.CurrentPlayer.Buffs != null)
                _client.Send(TerrPacketType.UpdatePlayerBuff,
                    _client.CurrentPlayer.Buffs.CreatePayload());

            if (_client.CurrentPlayer.Inventory != null)
                for (byte i = 0; i < PlayerInventory.InventorySize; i++)
                    _client.Send(TerrPacketType.SetInventory,
                        _client.CurrentPlayer.Inventory.InternalItems[i].CreatePayload());

            _client.Send(TerrPacketType.RequestWorldInformation);

            _client.Send(TerrPacketType.RequestInitialTileData,
                new ValPair<uint>(uint.MaxValue, uint.MaxValue).CreatePayload());

            _client.Send(TerrPacketType.SpawnPlayer,
                new ValPidPair<ushort>(ushort.MaxValue, ushort.MaxValue, _client.CurrentPlayer.PlayerId.Value)
                    .CreatePayload());

            _client.IsLoggingIn = false;
            _client.IsLoggedIn = true;

            // send keep alives. for now we use packet id 15's as keepalvies
            Task.Run(async () =>
            {
                while (_client.IsConnected)
                {
                    _client.Send(TerrPacketType.NullNeverSent);
                    await Task.Delay(_client.Config.KeepaliveFrequencyMs);
                }
            });
        }
    }
}
