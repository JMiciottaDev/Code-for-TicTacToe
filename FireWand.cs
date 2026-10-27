using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class FireWand : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject gamePiecePrefab;
    public Transform firePoint;
    public float shotSpeed = 20f;



    public XRNode controllerHand = XRNode.RightHand;

    [Header("Audio")]
    public AudioClip clip;
    private AudioSource source;

    private InputDevice controller;
    private bool wasTriggerPressed;

    void Update()
    {
        if (!controller.isValid)
        {
            controller = InputDevices.GetDeviceAtXRNode(controllerHand);
        }

        if (controller.isValid && controller.IsPressed(InputHelpers.Button.Trigger, out bool triggerPressed))
        {
            if (triggerPressed && !wasTriggerPressed)
            {
                FireProjectile();
            }

            wasTriggerPressed = triggerPressed;
        }
    }


    private void Start()
    {
        source = GetComponent<AudioSource>();

        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }
    }

    public void FireProjectile()
    {
        GameObject gamePiece = Instantiate(gamePiecePrefab, firePoint.position, firePoint.rotation);

        Rigidbody rb = gamePiece.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = firePoint.forward * shotSpeed;
        }

        // Play sound
        if (clip != null && source != null)
        {
            source.PlayOneShot(clip);
        }
    }
}
