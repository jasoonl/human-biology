#ifndef HBE_ANATOMY_CLIPPING_INCLUDED
#define HBE_ANATOMY_CLIPPING_INCLUDED

// Phase 24 (Boolean Clipping) + Phase 22 (Cutaway Masking Sphere).
// Globals pushed by RuntimeSlicer.cs / VolumetricCutawayController.cs.
float3 _SlicePlanePosition;
float3 _SlicePlaneNormal;
float _SliceActive;

float3 _CutawaySphereCenter;
float _CutawaySphereRadius;
float _CutawayActive;

// Discards the fragment if it is on the clipped side of the active slice plane
// and/or inside the active cutaway sphere. invertPlane flips which side is kept,
// used by the backface cap shader (Phase 25) to render only the interior face.
void ApplyAnatomyClipping(float3 worldPos, bool invertPlane)
{
    if (_SliceActive > 0.5)
    {
        float side = dot(worldPos - _SlicePlanePosition, _SlicePlaneNormal);
        if (invertPlane) side = -side;
        clip(side);
    }

    if (_CutawayActive > 0.5)
    {
        float distSq = dot(worldPos - _CutawaySphereCenter, worldPos - _CutawaySphereCenter);
        clip(distSq - (_CutawaySphereRadius * _CutawaySphereRadius));
    }
}

#endif
