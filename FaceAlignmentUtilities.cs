using UnityEngine;

public static class FaceAlignMath
{
    public static Quaternion ComputeFaceToFaceRotation(
        Vector3 originN, Vector3 originT,
        Vector3 targetN, Vector3 targetT)
    {
        originN = originN.normalized;
        targetN = targetN.normalized;

        // 1) Align normals
        Quaternion qNormal = Quaternion.FromToRotation(originN, targetN);

        // 2) Align tangents (twist) around target normal
        Vector3 tOrigin2 = qNormal * originT.normalized;

        Vector3 tO = Vector3.ProjectOnPlane(tOrigin2, targetN).normalized;
        Vector3 tT = Vector3.ProjectOnPlane(targetT.normalized, targetN).normalized;

        if (tO.sqrMagnitude < 1e-8f || tT.sqrMagnitude < 1e-8f)
        {
            return qNormal; // fallback: no twist if degenerate
        }

        float angle = Vector3.SignedAngle(tO, tT, targetN);
        Quaternion qTwist = Quaternion.AngleAxis(angle, targetN);

        return qTwist * qNormal;
    }

    public static void ApplyRotationAroundPivot(Transform tr, Vector3 pivot, Quaternion q)
    {
        Vector3 p = tr.position;
        tr.position = pivot + q * (p - pivot);
        tr.rotation = q * tr.rotation;
    }
}