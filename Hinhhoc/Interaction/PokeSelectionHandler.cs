using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(GeometryObject))]
public class PokeSelectionHandler : MonoBehaviour
{
    private GeometryObject geo;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;

    public InteractionCore core;

    private bool alreadySelected = false;

    void Awake()
    {
        geo = GetComponent<GeometryObject>();

        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        if (interactable == null)
        {
            interactable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        }

        interactable.selectEntered.AddListener(OnPoke);
    }

    void Start()
    {
        if (core == null)
        {
            core = FindObjectOfType<InteractionCore>();
        }
    }

    void OnPoke(SelectEnterEventArgs args)
    {
        if (alreadySelected) return;
        if (geo == null) return;

        alreadySelected = true;

        if (core == null) core = FindObjectOfType<InteractionCore>();
        if (core != null) core.SelectObject(geo);

        Debug.Log("[Poke] Đã chọn: " + gameObject.name);
    }

    public void ResetSelection()
    {
        alreadySelected = false;
    }

    void OnDestroy()
    {
        if (interactable != null) interactable.selectEntered.RemoveListener(OnPoke);
    }
}