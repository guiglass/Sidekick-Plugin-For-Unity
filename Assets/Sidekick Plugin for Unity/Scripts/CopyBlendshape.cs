//------------------------------------------------------------------------------
// Written by Animation Prep Studio
// www.mocapfusion.com
//------------------------------------------------------------------------------
using UnityEngine;

public class CopyBlendshape : MonoBehaviour
{
    [Range(0.0f,0.99f)]
    public float smoothing = 0.0f;
    
    public SkinnedMeshRenderer sourceRenderer;
    public int sourceBlendshapeIndex = 0;
    public SkinnedMeshRenderer destRenderer;
    public int destBlendshapeIndex = 0;
    
    void LateUpdate()
    {
        var valueA = sourceRenderer.GetBlendShapeWeight(sourceBlendshapeIndex);
        var valueB = destRenderer.GetBlendShapeWeight(destBlendshapeIndex);
        destRenderer.SetBlendShapeWeight(destBlendshapeIndex, Mathf.Lerp(valueA, valueB, smoothing));
    }
}
