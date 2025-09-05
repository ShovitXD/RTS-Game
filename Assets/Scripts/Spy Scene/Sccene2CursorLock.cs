using UnityEngine;

public class Scene2CursorLock : MonoBehaviour
{
    [Header("Policy (Scene 2 only)")]
    [SerializeField] private bool lockOnStart = true;      // lock when this scene starts
    [SerializeField] private bool unlockOnDisable = true;  // unlock when leaving this scene
    [SerializeField] private bool toggleWithEscape = true; // press Esc to unlock for UI

    void Start()
    {
        if (lockOnStart) Lock();
    }

    void Update()
    {
        if (toggleWithEscape && Input.GetKeyDown(KeyCode.Escape))
            Unlock();
    }

    void OnDisable()
    {
        if (unlockOnDisable) Unlock();
    }

    public void Lock()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Unlock()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
