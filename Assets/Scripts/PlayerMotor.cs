using UnityEngine;
using UnityEngine.InputSystem;

namespace ForestCleanup {
[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour {
    public Camera view;
    public float walkSpeed = 3, runSpeed = 5, sensitivity = .09f;
    [Header("Stamina")]
    [Min(.1f)] public float sprintDuration = 6f;
    [Min(0)] public float staminaRecoveryDelay = 2f;
    [Min(.1f)] public float staminaRecoveryDuration = 4f;
    [Min(0)] public float staminaBarHideDelay = 1.5f;
    public float Stamina { get; private set; } = 1f;
    public bool IsSprinting { get; private set; }
    public bool StaminaBarVisible { get; private set; }
    CharacterController controller;
    float pitch, fall, step;
    float recoveryWait, fullBarWait;
    bool exhausted;
    void Awake() { controller = GetComponent<CharacterController>(); }
    void Update() {
        if (!GameSession.Current.Playing || GameSession.Current.Paused) return;
        var k = Keyboard.current; var m = Mouse.current;
        if (k == null || m == null) return;
        Vector2 delta = m.delta.ReadValue() * sensitivity;
        transform.Rotate(0, delta.x, 0);
        pitch = Mathf.Clamp(pitch - delta.y, -80, 80);
        view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        Vector2 input = new Vector2((k.dKey.isPressed ? 1 : 0)-(k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0)-(k.sKey.isPressed ? 1 : 0));
        input = Vector2.ClampMagnitude(input, 1);
        TickStamina(k.leftShiftKey.isPressed, input.sqrMagnitude > .01f, Time.deltaTime);
        float speed = IsSprinting ? runSpeed : walkSpeed;
        fall = controller.isGrounded ? -2 : fall - 20 * Time.deltaTime;
        controller.Move(((transform.right * input.x + transform.forward * input.y)*speed + Vector3.up*fall)*Time.deltaTime);
        if (input.sqrMagnitude > .1f && controller.isGrounded) { step -= Time.deltaTime; if(step <= 0) { SoundBank.Play(transform.position, 100, .06f, .09f); step = IsSprinting ? .32f : .5f; } }
    }
    internal void TickStamina(bool sprintHeld, bool moving, float deltaTime) {
        if (!GameSession.Current.Playing || GameSession.Current.Paused) return;
        if (!sprintHeld) exhausted = false;
        if (sprintHeld) {
            StaminaBarVisible = true;
            fullBarWait = staminaBarHideDelay;
        }
        IsSprinting = sprintHeld && moving && !exhausted && Stamina > 0;
        if (IsSprinting) {
            Stamina = Mathf.Max(0, Stamina - deltaTime / Mathf.Max(.1f, sprintDuration));
            recoveryWait = staminaRecoveryDelay;
            fullBarWait = staminaBarHideDelay;
            if (Stamina <= 0) {
                // Require a new Shift press after exhaustion to avoid alternating run/walk frames.
                exhausted = true;
                IsSprinting = false;
            }
            return;
        }
        float recoveryTime = Mathf.Max(0, deltaTime - recoveryWait);
        recoveryWait = Mathf.Max(0, recoveryWait - deltaTime);
        if (Stamina < 1f) {
            Stamina = Mathf.Min(1f, Stamina + recoveryTime / Mathf.Max(.1f, staminaRecoveryDuration));
            fullBarWait = staminaBarHideDelay;
        } else if (!sprintHeld) {
            fullBarWait = Mathf.Max(0, fullBarWait - deltaTime);
            if (fullBarWait <= 0) StaminaBarVisible = false;
        }
    }
    public void Teleport(Vector3 p) { controller.enabled = false; transform.position = p; controller.enabled = true; fall = 0; }
}
public static class SoundBank {
    public static void Play(Vector3 position, float frequency, float volume, float duration) {
        int count = (int)(22050 * duration); var samples = new float[count];
        for(int i=0;i<count;i++) samples[i] = Mathf.Sin(i * frequency * Mathf.PI * 2 / 22050) * (1f-(float)i/count);
        var clip = AudioClip.Create("Procedural sound", count, 1, 22050, false); clip.SetData(samples,0);
        var go = new GameObject("One shot"); go.transform.position = position;
        var source = go.AddComponent<AudioSource>(); source.clip=clip; source.volume=volume; source.spatialBlend=1; source.minDistance=2; source.maxDistance=25; source.rolloffMode=AudioRolloffMode.Linear; source.Play();
        Object.Destroy(go,duration+.1f); Object.Destroy(clip,duration+.2f);
    }
}
}
