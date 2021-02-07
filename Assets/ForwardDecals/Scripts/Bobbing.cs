using UnityEngine;

public class Bobbing : MonoBehaviour {
    public float dist = 1.0f;
    public float speed = 1.0f;
    public bool animate = false;
    public Vector3 direction = Vector3.right;

    Vector3 originalPosition;

    void Awake() {
        originalPosition = transform.position;
    }

    void Update() {
        if(animate) {
            float offset = Mathf.Sin(Time.time * speed) * dist;
            transform.position = originalPosition + direction * offset;
        }
    }
}
