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

  // If true, will update static decals while in the editor (else they only position upon initial build).
  [SerializeField] bool updateStaticInEditor = true;

  // Should debug messages be printed?
  [SerializeField] bool printDebug = true;

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
  CommandBuffer staticBuffer;
  HashSet<ForwardDecal> staticDecals = new HashSet<ForwardDecal>();

  // Dynamic decals.
  CommandBuffer dynamicBuffer;
  HashSet<ForwardDecal> dynamicDecals = new HashSet<ForwardDecal>();

  void Awake() {
    // Create the fallback mesh.
    if(null == fallbackMesh) {
      var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
      fallbackMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
      GameObject.DestroyImmediate(tmp);

      Log("Created fallback mesh.");
    }

    Rebuild();
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
      Log("Creating new static buffer.");
      staticBuffer = new CommandBuffer();
      staticBuffer.name = "ForwardDecalSystem_StaticBuffer";
    }

    if(null == dynamicBuffer) {
      Log("Creating new dynamic buffer.");
      dynamicBuffer = new CommandBuffer();
      dynamicBuffer.name = "ForwardDecalSystem_DynamicBuffer";
    }
  }

  void UpdateStaticBuffer() {
    if(null == staticBuffer) {
      LogError("updating static buffer when it didn't exist.");
      return;
    }
    // Log($"Updating existing static buffer ({staticDecals.Count} decals).");

    if(0 == staticDecals.Count) {
      staticBuffer.Clear(); // Empty buffer.
    } else {
      // Clear any previous commands.
      staticBuffer.Clear();

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
    if(null == dynamicBuffer) {
      LogError("updating dynamic buffer when it didn't exist.");
      return;
    }
    // Log($"Updating existing dynamic buffer ({dynamicDecals.Count} decals).");

    if(0 == dynamicDecals.Count) {
      dynamicBuffer.Clear(); // Empty buffer.
    } else {
      dynamicBuffer.Clear();

      var screenTexID = Shader.PropertyToID("ForwardDecalSystem_Dynamc_ScreenTex");
      dynamicBuffer.GetTemporaryRT(screenTexID, -1, -1);
      dynamicBuffer.Blit(BuiltinRenderTextureType.CameraTarget, screenTexID);
      dynamicBuffer.SetGlobalTexture("_FDS_ScreenTex", screenTexID);

      dynamicBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);

      foreach(var decal in dynamicDecals) {
        if(null == decal || null == decal.Material) {
          LogError($"when rendering dynamic decal {decal.name}.");
          continue;
        }
        var mesh = (null == decal.Mesh) ? fallbackMesh : decal.Mesh;
        dynamicBuffer.DrawMesh(mesh, decal.transform.localToWorldMatrix, decal.Material);
      }

      dynamicBuffer.ReleaseTemporaryRT(screenTexID);
    }
  }

  void RemoveBuffersFromCamera(Camera cam, CameraEvent cameraEvent, bool includeStatic, bool includeDynamic) {
    if(null == staticBuffer || null == dynamicBuffer) {
      LogError($"removing buffers from {cam.name}; static={staticBuffer}, dynamic={dynamicBuffer}.");
      return;
    }

    var existing = cam.GetCommandBuffers(cameraEvent);

    if(includeStatic) {
      // // Try to remove the real one.
      // cam.RemoveCommandBuffer(cameraEvent, staticBuffer);
      // // Seems it's not always recognized, so the editor may 
      if(existing.FirstOrDefault(x => x.name == staticBuffer.name) != null) {
        Log($"Removing static buffer from {cam.name} at {cameraEvent}.");
        cam.RemoveCommandBuffer(cameraEvent, staticBuffer);
      } else {
        LogError($"removing static buffer from {cam.name} as it didn't exist.");
      }
    }

    if(includeDynamic) {
      if(existing.FirstOrDefault(x => x.name == dynamicBuffer.name) != null) {
        Log($"Removing dynamic buffer from {cam.name} at {cameraEvent}.");
        cam.RemoveCommandBuffer(cameraEvent, dynamicBuffer);
      } else {
        LogError($"removing dynamic buffer from {cam.name} as it didn't exist.");
      }
    }
  }

  void RemoveBuffersFromDecalCameras() {
    foreach(var decalCam in allCameras) {
      if(null == decalCam.camera) {
        continue;
      }
      RemoveBuffersFromCamera(decalCam.camera, decalCam.cameraEvent, decalCam.renderStatic, decalCam.renderDynamic);
    }
  }

  void AssignBuffersToCamera(Camera cam, CameraEvent cameraEvent, bool includeStatic, bool includeDynamic) {
    if(null == staticBuffer || null == dynamicBuffer) {
      LogError($"assigning buffers to {cam.name}; static={staticBuffer}, dynamic={dynamicBuffer}.");
      return;
    }

    var existing = cam.GetCommandBuffers(cameraEvent);

    if(includeStatic) {
      if(existing.FirstOrDefault(x => x.name == staticBuffer.name) != null) {
        LogError($"assigning static buffer to {cam.name} as it already existed.");
      } else {
        Log($"Assigning static buffer to {cam.name} at {cameraEvent}.");
        cam.AddCommandBuffer(cameraEvent, staticBuffer);
      }
    }

    if(includeDynamic) {
      if(existing.FirstOrDefault(x => x.name == dynamicBuffer.name) != null) {
        LogError($"assigning dynamic buffer to {cam.name} as it already existed.");
      } else {
        Log($"Assigning dynamic buffer to {cam.name} at {cameraEvent}.");
        cam.AddCommandBuffer(cameraEvent, dynamicBuffer);
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

  public void ReassignBuffers() {
    if(null == staticBuffer || null == dynamicBuffer) {
      return;
    }

    RemoveBuffersFromDecalCameras();
    AssignBuffersToDecalCameras();
  }

  public void Rebuild() {
    // Clear initial decal lists.
    staticDecals.Clear();
    dynamicDecals.Clear();

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
  }

  public void Add(ForwardDecal decal) {
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

  void OnEnable() {
    Rebuild(); // Editor can trigger this on script rebuilds and can nullify buffers, so just rebuild it.
  }

  void OnDisable() {
    RemoveBuffersFromDecalCameras();
  }

  void LateUpdate() {
    // Rebuild static when in the editor so that the preview updates. In a build, this won't happen unless manually updated.
    #if UNITY_EDITOR
    if(updateStaticInEditor) {
      UpdateStaticBuffer();
    }
    #endif

    // Rebuild every frame. Ideally this would rebuild as an observer or event system when a constituent changes, but that's for another time.
    UpdateDynamicBuffer();
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
