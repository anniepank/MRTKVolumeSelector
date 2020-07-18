using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class MRTKVolumeSelectorHandle : MonoBehaviour, IMixedRealityPointerHandler
{
    public Transform ParentTransform;

    [Tooltip("Minimum resize scale allowed.")]
    [SerializeField]
    public float MinScale = 0.5f;

    [Tooltip("Maximum resize scale allowed.")]
    [SerializeField]
    public float MaxScale = 4f;

    private Vector3 startParentScale;
    private Vector3 startParentPosition;
    private Vector3 pointerStartPosition;


    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        var dv = eventData.Pointer.Position - pointerStartPosition;
        Resize(dv);
    }

    private void Update()
    {
        var globalScale = Vector3.one * 0.02f;
        transform.localScale = new Vector3(
            globalScale.x / ParentTransform.lossyScale.x,
            globalScale.y / ParentTransform.lossyScale.y,
            globalScale.z / ParentTransform.lossyScale.z
            );
    }

    void Resize(Vector3 delta)
    {
        delta = Quaternion.Euler(-transform.eulerAngles) * delta;

        delta = new Vector3(
            Mathf.Abs(transform.localPosition.normalized.x) * delta.x, 
            Mathf.Abs(transform.localPosition.normalized.y) * delta.y, 
            Mathf.Abs(transform.localPosition.normalized.z) * delta.z
        );
        var size = (transform.position - ParentTransform.position) * 2;
        size = new Vector3(
            size.x > 0 ? size.x : 1,
            size.y > 0 ? size.y : 1,
            size.z > 0 ? size.z : 1
        );
        var newScale = new Vector3(delta.x / size.x, delta.y / size.y, delta.z / size.z);
        float resizeX, resizeY, resizeZ;
        resizeX = newScale.x * transform.localPosition.normalized.x;
        resizeY = newScale.y * transform.localPosition.normalized.y;
        resizeZ = newScale.z * transform.localPosition.normalized.z;

        resizeX = Mathf.Clamp(startParentScale.x + resizeX, MinScale, MaxScale);
        resizeY = Mathf.Clamp(startParentScale.y + resizeY, MinScale, MaxScale);
        resizeZ = Mathf.Clamp(startParentScale.z + resizeZ, MinScale, MaxScale);

        var resize = new Vector3(resizeX, resizeY, resizeZ);
        ParentTransform.localScale = resize;

        var positionChange =  (resize - startParentScale) / 2;
        positionChange.x *= Mathf.Sign(delta.x);
        positionChange.y *= Mathf.Sign(delta.y);
        positionChange.z *= Mathf.Sign(delta.z);

        positionChange = Quaternion.Inverse(Quaternion.Euler(-transform.eulerAngles)) * positionChange;
        var newPosition = startParentPosition + positionChange;
        ParentTransform.position = newPosition; 
    }


    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        startParentScale = ParentTransform.localScale;
        pointerStartPosition = eventData.Pointer.Position;
        startParentPosition = ParentTransform.position;
    }


    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
    }
}
