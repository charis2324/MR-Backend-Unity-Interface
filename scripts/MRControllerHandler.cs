using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class MRControllerHandler : MonoBehaviour
{
    private MRController mrController;
    private CancellationTokenSource cancellationTokenSource;
    private Task connectionTask;
    // Start is called before the first frame update
    void Start()
    {
        // Add action for the event.
        ImportFurnitureEventCommand.OnEvent += (uuid) =>
        {
            Debug.Log($"Furniture with Uuid: {uuid} is loaded.");
        };
        mrController = new MRController("http://127.0.0.1:8000", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhc2QiLCJleHAiOjE2OTc2MTk4Njh9.Qxw3OjpogesI-GQgd61wGHWf5Pg6z6Xin4G9pbGH32Q");
        cancellationTokenSource = new CancellationTokenSource();
        connectionTask = mrController.Connect(cancellationTokenSource.Token);
    }
    private async void OnDestroy()
    {
        await CleanUp();
    }

    private async Task CleanUp()
    {
        if (mrController != null)
        {
            cancellationTokenSource.Cancel();
        }
        if (!connectionTask.IsCompleted)
        {
            await connectionTask;
        }
    }
}
