using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDGoal : MonoBehaviour
{
    public GameObject goalIndicator;
    public RawImage goalArrow;
    public RawImage goalCenter;
    public Image goalProgress;

    private void Awake()
    {
        goalIndicator.SetActive(false);
    }

    static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z)
               && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
    }

    static bool TryProjectToScreen(Camera camera, Vector3 worldPosition, out Vector3 screenPosition)
    {
        screenPosition = default;
        var viewProjection = camera.projectionMatrix * camera.worldToCameraMatrix;
        var clipPosition = viewProjection * new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, 1.0f);

        if (Mathf.Abs(clipPosition.w) < 1e-5f)
            return false;

        var invW = 1.0f / clipPosition.w;
        var ndcX = clipPosition.x * invW;
        var ndcY = clipPosition.y * invW;
        var ndcZ = clipPosition.z * invW;

        if (!float.IsFinite(ndcX) || !float.IsFinite(ndcY) || !float.IsFinite(ndcZ))
            return false;

        screenPosition = new Vector3(
            (ndcX * 0.5f + 0.5f) * camera.pixelWidth,
            (ndcY * 0.5f + 0.5f) * camera.pixelHeight,
            clipPosition.w);

        return clipPosition.w > 0.0f;
    }

    public void FrameUpdate(LocalPlayer localPlayer)
    {
        if(!localPlayer.playerState.displayGoal)
        {
            goalIndicator.SetActive(false);
            return;
        }
        goalIndicator.SetActive(true);
        var goalPosition = localPlayer.playerState.goalPosition;
        if (!IsFinite(goalPosition))
        {
            goalIndicator.SetActive(false);
            return;
        }

        var c = Game.game.TopCamera();
        if (c == null || !c.enabled)
        {
            goalIndicator.SetActive(false);
            return;
        }

        if (!IsFinite(c.transform.position) || !IsFinite(c.transform.forward) || !IsFinite(c.transform.right))
        {
            goalIndicator.SetActive(false);
            return;
        }

        if (!TryProjectToScreen(c, goalPosition, out var sp))
        {
            goalIndicator.SetActive(false);
            return;
        }

        sp.z = 0;
        sp.x = sp.x / Screen.width - 0.5f;
        sp.y = sp.y / Screen.height - 0.5f;

        float sp_mag = sp.magnitude;
        if (sp_mag > 1.0f)
            sp /= sp_mag;

        float dot = Vector3.Dot(c.transform.forward, (goalPosition - c.transform.position).normalized);
        if (dot < 0.25f)
        {
            float blend = Mathf.Clamp01(dot * 4.0f);
            float lr = Vector3.Dot(c.transform.right, (goalPosition - c.transform.position).normalized);
            Vector3 behind_sp = new Vector3(lr * 0.5f, -0.5f, 0);
            sp = Vector3.Lerp(behind_sp, sp, blend);
        }

        float arrowDirection = 180.0f;

        var inner = sp;
        inner.x = Mathf.Clamp(sp.x, -0.3f, 0.3f);
        inner.y = Mathf.Clamp(sp.y, -0.2f, 0.3f);

        if (dot < 0.0f || (sp-inner).magnitude > 0.15f)
        {
            // If outside center area of screen, show arrow pointing in direction of goal
            var d = sp.normalized;
            arrowDirection = Mathf.Atan2(-d.x, d.y) * 180.0f / Mathf.PI;
            sp = inner + (sp - inner).normalized * 0.15f;
            goalArrow.enabled = true;
            goalCenter.enabled = false;
        }
        else
        {
            goalCenter.enabled = true;
            goalArrow.enabled = false;
        }

        sp.x = (sp.x + 0.5f ) * Screen.width;
        sp.y = (sp.y + 0.5f ) * Screen.height;

        if (sp.x < 1.0f || sp.x > Screen.width - 1.0f || sp.y < 1.0f || sp.y > Screen.height - 1.0f)
        {
            goalIndicator.SetActive(false);
            return;
        }

        goalIndicator.transform.position = sp;
        var la = goalArrow.transform.localEulerAngles;
        la.z = arrowDirection;
        goalArrow.transform.localEulerAngles = la;
        goalArrow.SetRGB(Game.game.gameColors[(int)localPlayer.playerState.goalDefendersColor]);
        goalCenter.SetRGB(Game.game.gameColors[(int)localPlayer.playerState.goalDefendersColor]);
        goalProgress.fillAmount = localPlayer.playerState.goalCompletion;
    }
}
