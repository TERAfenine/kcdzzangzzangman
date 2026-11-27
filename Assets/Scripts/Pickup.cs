using UnityEngine;
using UnityEngine.InputSystem;
namespace ForestCleanup {
public class Pickup : MonoBehaviour {
    public bool Collected { get; private set; }
    public void Collect() {
        if (Collected || !GameSession.Current.Playing) return;
        Collected = true; gameObject.SetActive(false); GameSession.Current.RecordPickup();
    }
}
public class Interaction : MonoBehaviour {
    public Camera view;
    public Pickup Target { get; private set; }
    void Update() {
        Target=null;
        if (!GameSession.Current.Playing || GameSession.Current.Paused) return;
        if (Physics.Raycast(view.transform.position,view.transform.forward,out var hit,2f,~(1<<2),QueryTriggerInteraction.Ignore)) Target=hit.collider.GetComponentInParent<Pickup>();
        if(Target != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) Target.Collect();
    }
}
}
