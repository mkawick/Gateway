using System;
using Newtonsoft.Json.Linq;
using Packets;
using UnityEngine;

class ClientGameStateStore : MonoBehaviour, IGameStateStore
{
#pragma warning disable 67
    public event EventHandler<GameStateStoreLoadResult> OnLoadCompleted;
    public event EventHandler<GameStateStoreSaveResult> OnSaveCompleted;
    public event EventHandler<GameStateStoreDeleteResult> OnDeleteCompleted;
#pragma warning restore 67

    private string lastLoadSaveName = null;

    private void Start()
    {
        GameClient.Instance.Client.AddListener<PlayerSaveStatePacket>(StateLoaded);
    }

    // GameClientComponent will wire this up to the IClientInterface.OnPlayerStateSaveReceived event.
    // We assume that Load will have been called already by the time this is fired.
    private void StateLoaded(PlayerSaveStatePacket packet)
    {
        string state = packet.state.state.state;
        // We need to capture the lastLoadSaveName before we null it out
        // so that the closures below work
        string saveName = lastLoadSaveName;
        try
        {
            JObject saveData = JObject.Parse(state);
            OnLoadCompleted?.Invoke(this, new GameStateStoreLoadResult(saveName, true, saveData));
        }
        catch (Exception)
        {
            OnLoadCompleted?.Invoke(this, new GameStateStoreLoadResult(saveName, false, null));
        }
        finally
        {
            lastLoadSaveName = null;
        }
    }

    public void Load(string saveName)
    {
        // The client can't request a load - they get given one, so just remember the save name
        lastLoadSaveName = saveName;
    }

    public void Save(string saveName, JObject json)
    {
#if DEBUG_SAVES
        Debug.Log(json);
#endif
        UpdatePlayerSaveStatePacket packet = (UpdatePlayerSaveStatePacket)IntrepidSerialize.TakeFromPool(PacketType.UpdatePlayerSaveState);
        packet.state = new PlayerSaveStateData();
        packet.state.state = json.ToString();
        GameClient.Instance.Client.Send(packet);
    }

    public void DeleteSave(string saveName)
    {
        // Not really a thing
        // Could translate to delete my character?
    }
}
