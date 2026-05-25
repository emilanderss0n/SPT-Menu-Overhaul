using UnityEngine;
using UnityEngine.EventSystems;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    // Mouse-drag rotation for the cloned main-menu player model.
    // Mirrors EFT.Utilities.XCoordRotation behavior (yaw -= delta * speed, wrapped to 0..360),
    // but polls Input directly so we don't have to attach a UI raycast region that would
    // intercept clicks on the main-menu buttons.
    internal class PlayerModelDragRotator : MonoBehaviour
    {
        public Transform Target;
        public float RotationSpeed = 0.3f;
        public float CurrentYaw;

        private bool _dragging;

        private void Update()
        {
            if (Target == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                EventSystem es = EventSystem.current;
                // Only start a drag when the click did not begin on a UI element
                // (so menu buttons keep working normally).
                if (es == null || !es.IsPointerOverGameObject())
                {
                    _dragging = true;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                _dragging = false;
            }

            if (!_dragging || !Input.GetMouseButton(0)) return;

            // Mouse X axis gives a smoothed per-frame delta. Scale roughly to pixel delta
            // so RotationSpeed behaves similarly to the matchmaking screen's 0.3f.
            float dx = Input.GetAxis("Mouse X") * 30f;
            if (Mathf.Approximately(dx, 0f)) return;

            CurrentYaw -= dx * RotationSpeed;
            if (CurrentYaw >= 360f) CurrentYaw -= 360f;
            else if (CurrentYaw < 0f) CurrentYaw += 360f;

            Target.localRotation = Quaternion.Euler(0f, CurrentYaw, 0f);
        }

        public void SetYaw(float yaw)
        {
            CurrentYaw = yaw;
            if (Target != null)
            {
                Target.localRotation = Quaternion.Euler(0f, CurrentYaw, 0f);
            }
        }
    }
}
