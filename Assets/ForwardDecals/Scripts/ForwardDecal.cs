using UnityEngine;

public class ForwardDecal : MonoBehaviour {
  [SerializeField] Material material;
  [SerializeField] Mesh customMesh;
  [SerializeField] bool isStatic = true;
  [SerializeField] ForwardDecalSystem decalSystem;

  public Material Material {
    get{ return material; }
  }

  public Mesh Mesh {
    get{ return customMesh; }
  }

  public bool IsStatic {
    get{ return isStatic; }
  }

  void OnDrawGizmos() {
    DrawGizmo(false);
  }

  void OnDrawGizmosSelected() {
    DrawGizmo(true);
  }

  void DrawGizmo(bool selected) {
    var mesh = (null == customMesh) ? decalSystem.FallbackMesh : customMesh;

    // Draw main mesh.
    var col = new Color(0.0f, 0.7f, 1.0f, 1.0f);
    col.a = selected ? 0.3f : 0.1f;
    Gizmos.color = col;
    Gizmos.matrix = transform.localToWorldMatrix;
    Gizmos.DrawMesh(mesh, Vector3.zero, Quaternion.identity, Vector3.one);

    // Draw outline wire mesh.
    col.a = selected ? 0.5f : 0.2f;
    Gizmos.color = col;
    Gizmos.DrawWireMesh(mesh, Vector3.zero, Quaternion.identity, Vector3.one);
  }
}
