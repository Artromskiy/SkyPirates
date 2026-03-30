using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DVG.SkyPirates.Tooling.Controls
{
    public class CameraController : MonoBehaviour
    {
        private float3? _prevWorldXZDrag;
        private float? _prevYDrag;

        private void Update()
        {
            SetDynamicShadowDistance();
            Drag();
            Zoom();
        }

        private void Drag()
        {
            var z = Input.GetAxisRaw("Vertical");
            var x = Input.GetAxisRaw("Horizontal");

            float3 camPos = Camera.main.transform.position;
            Camera.main.transform.position = camPos + new float3(x, 0, z) * Time.deltaTime * 10;

            if (!Input.GetMouseButton(2) || Input.GetKey(KeyCode.LeftControl))
            {
                _prevWorldXZDrag = null;
                return;
            }

            var current = GetWorldXZ();
            if (_prevWorldXZDrag.HasValue)
            {
                var delta = _prevWorldXZDrag.Value - current;
                camPos = Camera.main.transform.position;
                Camera.main.transform.position = camPos + delta;
            }
            _prevWorldXZDrag = GetWorldXZ();
        }

        private void Zoom()
        {
            float3 camPos = Camera.main.transform.position;
            float2 scroll = Input.mouseScrollDelta;
            var scrollDelta = Maths.Abs(scroll.x) > Maths.Abs(scroll.y) ? scroll.x : scroll.y;
            var delta = new float3(-scrollDelta * Time.deltaTime);
            delta *= camPos.y;
            delta.x = 0;
            delta.z = -delta.y;
            Camera.main.transform.position = camPos + delta;

            if (!Input.GetMouseButton(2) || !Input.GetKey(KeyCode.LeftControl))
            {
                _prevYDrag = null;
                return;
            }

            var current = Input.mousePosition.y / Screen.height;
            if (_prevYDrag.HasValue)
            {
                delta = new float3(_prevYDrag.Value - current);
                camPos = Camera.main.transform.position;
                delta *= camPos.y;
                delta.x = 0;
                delta.z = -delta.y;
                Camera.main.transform.position = camPos + delta;
            }
            _prevYDrag = current;
        }

        private void SetDynamicShadowDistance()
        {
            float minHeight = -5;
            var ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height));
            new Plane(Vector3.down, minHeight).Raycast(ray, out var enter);
            QualitySettings.shadowDistance = enter;
            UniversalRenderPipelineAsset urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            urp.shadowDistance = enter;
        }

        private float3 GetWorldXZ()
        {
            var pos = Input.mousePosition;
            var ray = Camera.main.ScreenPointToRay(pos);
            new Plane(Vector3.down, 0).Raycast(ray, out var enter);
            return ray.origin + ray.direction * enter;
        }
    }
}
