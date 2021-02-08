using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

[ExecuteInEditMode]
[DefaultExecutionOrder(-99999)]
public class ForwardDecalSystem : MonoBehaviour {
  [Serializable]
  class DecalCamera {
    public Camera camera = null;
    public CameraEvent cameraEvent = CameraEvent.AfterEverything;
    public bool renderStatic = true;
    public bool renderDynamic = true;
  }

  // Should debug messages be printed?
  [SerializeField] bool printDebug = true;
  
  // If true, will update static decals while in the editor (else they only position upon initial build).
  [SerializeField] bool updateStatic = true;

  // All cameras that will render decals. CommandBuffers are assigned on rebuild.
  [SerializeField] List<DecalCamera> allCameras = new List<DecalCamera>();

  // All decals that will be rendered. Automatically gets split into static/dynamic batches.
  [SerializeField] List<ForwardDecal> allDecals = new List<ForwardDecal>();

  // If a decal doesn't have custom geometry, this mesh is used instead.
  Mesh fallbackMesh = null;
  public Mesh FallbackMesh {
    get{ return fallbackMesh; }
  }

  // Static decals.
  const string STATIC_BUFFER_NAME = "ForwardDecalSystem_StaticBuffer";
  HashSet<ForwardDecal> staticDecals = new HashSet<ForwardDecal>();
  CommandBuffer staticBuffer;

  // Dynamic decals.
  const string DYNAMIC_BUFFER_NAME = "ForwardDecalSystem_DynamicBuffer";
  HashSet<ForwardDecal> dynamicDecals = new HashSet<ForwardDecal>();
  CommandBuffer dynamicBuffer;

  bool initialized = false; // Set in OnEnable() to prevent issues with OnValidate().

  void OnEnable() {
    EnableDepth();
    CreateFallbackMesh();
    Rebuild();
    initialized = true;
  }

  void OnDisable() {
    RemoveBuffersFromDecalCameras();
    initialized = false;
  }

  void OnValidate() {
    // Unity triggers this function on script reload and inspector value change.
    // This check avoids calling at the wrong time before OnEnabled() is triggered.
    if(initialized) {
      Rebuild();
    }
  }

  void LateUpdate() {
    if(updateStatic) {
      UpdateStaticBuffer();
    }
    UpdateDynamicBuffer();
  }

  void EnableDepth() {
    foreach(var decalCam in allCameras) {
      if(decalCam.camera != null) {
        decalCam.camera.depthTextureMode = DepthTextureMode.Depth;
      }
    }
  }

  void CreateFallbackMesh() {
    // Create the fallback mesh.
    if(null == fallbackMesh) {
      var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
      fallbackMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
      GameObject.DestroyImmediate(tmp);

      Log("Created fallback mesh.");
    }
  }

  public void Rebuild() {
    // Clear initial decal lists.
    staticDecals = new HashSet<ForwardDecal>();
    dynamicDecals = new HashSet<ForwardDecal>();

    // Populate initial decal lists.
    PopulateDecalLists();

    // Build initial buffers.
    BuildBuffers();

    // Update initial decals.
    UpdateStaticBuffer();
    UpdateDynamicBuffer();

    // (Re)assign buffers to cameras accordingly.
    RemoveBuffersFromDecalCameras();
    AssignBuffersToDecalCameras();

    Log($"Rebuild complete with {staticDecals.Count} static and {dynamicDecals.Count} dynamic decals.");
  }

  void PopulateDecalLists() {
    foreach(var decal in allDecals) {
      if(null == decal) {
        continue;
      }

      if(decal.IsStatic) {
        staticDecals.Add(decal);
      } else {
        dynamicDecals.Add(decal);
      }
    }
  }

    void BuildBuffers() {
    if(null == staticBuffer) {
      staticBuffer = new CommandBuffer();
      staticBuffer.name = ForwardDecalSystem.STATIC_BUFFER_NAME;
      Log("Created new static buffer.");
    }

    if(null == dynamicBuffer) {
      dynamicBuffer = new CommandBuffer();
      dynamicBuffer.name = ForwardDecalSystem.DYNAMIC_BUFFER_NAME;
      Log("Created new dynamic buffer.");
    }
  }

  void UpdateStaticBuffer() {
    if(null == fallbackMesh) {
      LogError("updating static buffer as fallback mesh was null.");
      return;
    }
    if(null == staticBuffer) {
      LogError("updating static buffer when it didn't exist.");
      return;
    }

    // Clear any previous commands.
    staticBuffer.Clear();

    if(staticDecals.Count > 0) {
      // Copy screen texture and set it globally.
      var screenTexID = Shader.PropertyToID("ForwardDecalSystem_Static_ScreenTex");
      staticBuffer.GetTemporaryRT(screenTexID, -1, -1);
      staticBuffer.Blit(BuiltinRenderTextureType.CameraTarget, screenTexID);
      staticBuffer.SetGlobalTexture("_FDS_ScreenTex", screenTexID);

      // Render to the screen.
      staticBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);

      // Render all static decals.
      foreach(var decal in staticDecals) {
        if(null == decal || null == decal.Material) {
          LogError($"when rendering static decal {decal.name}.");
          continue;
        }
        var mesh = (null == decal.Mesh) ? fallbackMesh : decal.Mesh;
        staticBuffer.DrawMesh(fallbackMesh, decal.transform.localToWorldMatrix, decal.Material);
      }

      // Clean up temporary RT.
      staticBuffer.ReleaseTemporaryRT(screenTexID);
    }
  }

  void UpdateDynamicBuffer() {
    if(null == fallbackMesh) {
      LogError("updating dynamic buffer as fallback mesh was null.");
      return;
    }
    if(null == dynamicBuffer) {
      LogError("updating dynamic buffer when it didn't exist.");
      return;
    }

    // Basically the same as updating the static buffer.

    dynamicBuffer.Clear();

    if(dynamicDecals.Count > 0) {
      var screenTexID = Shader.PropertyToID("ForwardDecalSystem_Dynamic_ScreenTex");
      dynamicBuffer.GetTemporaryRT(screenTexID, -1, -1);
      dynamicBuffer.Blit(BuiltinRenderTextureType.CameraTarget, screenTexID);
      dynamicBuffer.SetGlobalTexture("_FDS_ScreenTex", screenTexID);

      dynamicBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);

      foreach(var decal in dynamicDecals) {
        if(null == decal || null == decal.Material) {
          LogError($"when rendering dynamic decal {decal.name}.");
          continue;
        }
        // Respect decals that are disabled in the hierarchy.
        if(!decal.gameObject.activeInHierarchy) {
          continue;
        }
        var mesh = (null == decal.Mesh) ? fallbackMesh : decal.Mesh;
        dynamicBuffer.DrawMesh(mesh, decal.transform.localToWorldMatrix, decal.Material);
      }

      dynamicBuffer.ReleaseTemporaryRT(screenTexID);
    }
  }

  void RemoveBuffersFromDecalCameras() {
    foreach(var decalCam in allCameras) {
      if(null == decalCam.camera) {
        continue;
      }
      RemoveBuffersFromCamera(decalCam.camera);
    }
  }
  

  void RemoveBuffersFromCamera(Camera cam) {
    if(null == staticBuffer || null == dynamicBuffer) {
      LogError($"removing buffers from {cam.name}; static={staticBuffer}, dynamic={dynamicBuffer}.");
      return;
    }

    // As the event can change in the inspector, we don't know which one was previous.
    // To make things easier, just remove all buffers with matching names.

    foreach(var evt in Enum.GetValues(typeof(CameraEvent))) {
      var allBuffers = cam.GetCommandBuffers((CameraEvent)evt);

      var existingStatic = allBuffers.FirstOrDefault(x => x.name == ForwardDecalSystem.STATIC_BUFFER_NAME);
      if(existingStatic != null) {
        cam.RemoveCommandBuffer((CameraEvent)evt, existingStatic);
        Log($"Removed static buffer from camera at {(CameraEvent)evt}.");
      }

      var existingDynamic = allBuffers.FirstOrDefault(x => x.name == ForwardDecalSystem.DYNAMIC_BUFFER_NAME);
      if(existingDynamic != null) {
        cam.RemoveCommandBuffer((CameraEvent)evt, existingDynamic);
        Log($"Removed dynamic buffer from camera at {(CameraEvent)evt}.");
      }
    }
  }

  void AssignBuffersToDecalCameras() {
    foreach(var decalCam in allCameras) {
      if(null == decalCam.camera) {
        continue;
      }
      AssignBuffersToCamera(decalCam.camera, decalCam.cameraEvent, decalCam.renderStatic, decalCam.renderDynamic);
    }
  }

  void AssignBuffersToCamera(Camera cam, CameraEvent cameraEvent, bool includeStatic, bool includeDynamic) {
    if(null == staticBuffer || null == dynamicBuffer) {
      LogError($"assigning buffers to {cam.name}; static={staticBuffer}, dynamic={dynamicBuffer}.");
      return;
    }

    var existing = cam.GetCommandBuffers(cameraEvent);

    if(includeStatic) {
      if(existing.FirstOrDefault(x => x.name == ForwardDecalSystem.STATIC_BUFFER_NAME) != null) {
        LogError($"assigning static buffer to {cam.name} as it already existed.");
      } else {
        cam.AddCommandBuffer(cameraEvent, staticBuffer);
        Log($"Assigned static buffer to {cam.name} at {cameraEvent}.");
      }
    }

    if(includeDynamic) {
      if(existing.FirstOrDefault(x => x.name == ForwardDecalSystem.DYNAMIC_BUFFER_NAME) != null) {
        LogError($"assigning dynamic buffer to {cam.name} as it already existed.");
      } else {
        cam.AddCommandBuffer(cameraEvent, dynamicBuffer);
        Log($"Assigned dynamic buffer to {cam.name} at {cameraEvent}.");
      }
    }
  }

  public void Add(ForwardDecal decal) {
    if(!initialized) {
      return;
    }

    if(decal.IsStatic && !staticDecals.Contains(decal)) {
      Log($"Adding static decal {decal.name}.");
      staticDecals.Add(decal);
      UpdateStaticBuffer();
    } else if(!dynamicDecals.Contains(decal)) {
      Log($"Adding dynamic decal {decal.name}.");
      dynamicDecals.Add(decal);
      UpdateDynamicBuffer();
    }
  }

  public void Remove(ForwardDecal decal) {
    if(!initialized) {
      return;
    }

    // Attempt to remove from both as if it's in a list and its static flag
    // is toggled, it wouldn't be removed from the old list if we check that flag.

    if(staticDecals.Remove(decal)) {
      Log($"Removing static decal {decal.name}.");
      UpdateStaticBuffer();
    }

    if(dynamicDecals.Remove(decal)) {
      Log($"Removing dynamic decal {decal.name}.");
      UpdateDynamicBuffer();
    }
  }

  void Log(string msg) {
    if(printDebug) {
      Debug.Log($"<color=#44bd32>Decal System:</color> {msg}");
    }
  }
  void LogError(string msg) {
    if(printDebug) {
      Log($"<color=#ff0000>Error</color> {msg}");
    }
  }
}
