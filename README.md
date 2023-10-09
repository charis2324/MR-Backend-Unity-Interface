# MR-Backend-Unity-Interface

# MRClientHandlerEditor

`MRClientHandlerEditor` is a custom editor script for the `MRClientHandler` class in Unity.
It provides a user interface in the Unity Editor for setting up and interacting with the `MRClientHandler` component.

In `MRClientHandlerEditor`:

- `Load OBJ File` button: When clicked, it loads an OBJ file from the system's file explorer, and assigns it to the `MRClientHandler` component.
- `Text Prompt` input field: It allows the user to input a string that will be sent to the `MRClientHandler` component.
- `Guidance Scale` input field: It sets a float value that will be passed to the `MRClientHandler` component.
- `Generate Model Mesh` button: When clicked, it triggers the `MRClientHandler` component to generate a new mesh.
- `Task ID` input field: It allows the user to input a task ID.
- `Check Task Status` button: When clicked, it fetches the status of the task with the ID entered in the `Task ID` input field.

# MRClientHandler

`MRClientHandler` is a component that wraps the MR service and provides a way to load an OBJ file, generate a new model mesh, and get the status of a task.

- `LoadOBJFromFileInEditor`: Opens a system file explorer to select an OBJ file, then loads the file and assigns it to the `MeshFilter` component of the GameObject.
- `LoadOBJFromFile`: Loads an OBJ file from a given path.
- `LoadOBJFromString`: Loads a mesh from an OBJ file content string.
- `OBJParseLines`: Parses an OBJ file lines and generates a Unity `Mesh`.
- `GenerateModelMesh`: Generates a new mesh based on a given prompt and guidance scale.
- `ApplyGeneratedMesh`: Applies a generated mesh to the GameObject.
- `GetTaskStatusById`: Fetches the status of a task with a given ID.

# MRServiceWrapper

`MRServiceWrapper` is a component that interacts with a MR service through HTTP requests. It provides functions to generate a new mesh, get the status of a task, and get the result of a task.

- `GenerateMeshAndStartPolling`: Sends a request to generate a new mesh, then starts polling the task status until it's completed.
- `GenerateMesh`: Sends a request to generate a new mesh.
- `StartPollingTaskStatus`: Starts polling the task status until it's completed.
- `PollTaskStatus`: Continuously fetches the status of a task until it's completed or failed.
- `GetTaskStatus`: Fetches the status of a task with a given ID.
- `GetTaskResult`: Fetches the result of a task with a given ID.
- `GetTaskPreview`: Fetches the preview of a task with a given ID.

# GenerationTaskRequest

A class representing a request to generate a new mesh.

- `prompt`: The text prompt for generating the new mesh.
- `guidance_scale`: The guidance scale for generating the new mesh.

# GenerationTaskResponse

A class representing a response from generating a new mesh task.

- `task_id`: The ID of the task.
- `estimated_duration`: The estimated duration of the task.

# TaskStatusEnum

An enum representing the status of a task. It includes the following statuses:

- `waiting`: The task is waiting to be processed.
- `processing`: The task is currently being processed.
- `completed`: The task has been completed.
- `failed`: The task has failed.

# TaskStatus

A class representing the status of a task.

- `task_id`: The ID of the task.
- `status`: The status of the task.
- `message`: A message about the task status.
