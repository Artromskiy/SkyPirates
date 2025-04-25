using Unity.Netcode;
using UnityEngine;

public class NetworkButtons : MonoBehaviour
{
    private void OnGUI()
    {
        using var _ = new GUILayout.AreaScope(new Rect(10, 10, 300, 300));

        var netManager = NetworkManager.Singleton;

        if (netManager == null || netManager.IsClient || netManager.IsServer)
            return;

        if (GUILayout.Button("Host"))
            netManager.StartHost();
        if (GUILayout.Button("Server"))
            netManager.StartServer();
        if (GUILayout.Button("Client"))
            netManager.StartClient();
    }

    // private void Awake() {
    //     GetComponent<UnityTransport>().SetDebugSimulatorParameters(
    //         packetDelay: 120,
    //         packetJitter: 5,
    //         dropRate: 3);
    // }
}