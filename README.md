# Unity GPU Debugger

## How to install
#### Unity Package Manager
1. Open the package manager (Window > Package Manager).
2. Press the big plus icon, select "Install package from git URL".
3. Paste the URL of the repo `https://github.com/stuw-u/GPUDebugger.git` (note the `.git` ending) and click install.

![image](https://github.com/pema99/GITweaks/assets/11212115/133bdd9c-7f87-4714-8b1f-ed5eece77c95)

## GPU Object Debugger
This tool can be opened in Window > Analysis > GPU Object Debugger, or by calling `GPUObjectDebuggerWindow.ShowObject();`. 
Objects will automatically show up if they are tracked with `GPUObjectDebugger.StartTracking(this);` and can be removed with `GPUObjectDebugger.StopTracking(this);`
The tracked objects do not have to be `Unity.Object`. The tool does not search fields recursively.

### Buffer Viewer
See a list of `GraphicsBuffer` or `ComputeBuffer` that can be parsed. Put the attribute `[GPUDebugAs(typeof(YourStruct))]` before your buffers to track them.
Once the Load button is clicked, the whole buffer will be downloaded allowing you to preview the fields.

![image](https://github.com/user-attachments/assets/7346cba6-be4e-479f-8bdf-42e50e14b82f)

### Texture Viewer
See a list of textures. Supports any type inheriting from `Texture` like `RenderTexture`. Put the attribute `[GPUDebug]` to make it show up. 
Click on Open to preview the texture.

![image](https://github.com/user-attachments/assets/62e1a760-73e6-4c5e-a909-51dbb77eacd1)

### Memory Usage
Estimates the VRAM memory usage of all the GPU objects. Entries are sorted by size and show the size in MB and in percentage from the whole object.

![image](https://github.com/user-attachments/assets/a7635780-18b2-4a54-9c85-6317f38e28d2)

### Debug Routines
Put the attribute `[GPUDebugRoutine]` on any parameterless method on your tracked object and have it show up. You can use this to run complex debugging routines.

![image](https://github.com/user-attachments/assets/8eaf3ca7-8ec4-4337-9a8c-f3abfb2de01b)

## Future Plans
- Bindless buffers DX12 plug-in for the editor that would allow Gizmos to be queue/drawn inside Compute Shaders
- Automatically register ScriptableRenderFeature and ScriptableRenderPasses as objects to be debugged.

## TODO
- Fix Texture Preview being squashed down when the window is smaller.
- Better way of tracking objects.
- Recursive field search.
- Improve the style of the editor.
- Buffer viewer paging system, some way to find items by index or go to any page.
