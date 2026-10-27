using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;

public class WandEffect : MonoBehaviour
{
    [Header("Controller Settings")]
    public XRNode controllerHand = XRNode.RightHand;

    [Header("Effect Settings")]
    public GameObject effectObject;

    private InputDevice controller;
    private bool wasPressed;

    void Start()
    {
        if (effectObject != null)
            effectObject.SetActive(false);
    }

    void Update()
    {
        if (!controller.isValid)
        {
            controller =
                InputDevices.GetDeviceAtXRNode(controllerHand);
        }

        if (!controller.isValid)
            return;

        bool isPressed = IsAnyButtonPressed();

        if (effectObject != null)
        {
            effectObject.SetActive(isPressed);
        }

        if (controllerHand == XRNode.RightHand)
        {
            if (controller.IsPressed(InputHelpers.Button.SecondaryButton,out bool bPressed))
            {
                if (bPressed)
                {
                    ReloadScene();
                    return;
                }
            }
        }
    }

    bool IsAnyButtonPressed()
    {
        InputHelpers.Button[] buttons =
        {
            InputHelpers.Button.Trigger,
            InputHelpers.Button.Grip,
            InputHelpers.Button.PrimaryButton,
            InputHelpers.Button.SecondaryButton,
            InputHelpers.Button.Primary2DAxisClick,
            InputHelpers.Button.MenuButton
        };

        foreach (InputHelpers.Button button in buttons)
        {
            if (controller.IsPressed(button, out bool pressed)
                && pressed)
            {
                return true;
            }
        }

        return false;
    }

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}