using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public class CameraTool
    {
        public static Vector4 GetPlane(Vector3 normal, Vector3 point)
        {
            return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, point));
        }

        public static Vector4 GetPlane(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            return GetPlane(normal, a);
        }

        public static Vector3[] GetCameraFarClipPlanePoint(Camera camera)
        {
            Vector3[] points = new Vector3[4];
            Transform transform = camera.transform;
            float distance = camera.farClipPlane;
            float halfFovRad = Mathf.Deg2Rad * camera.fieldOfView * 0.5f;
            float upLen = distance * Mathf.Tan(halfFovRad);
            float rightLen = upLen * camera.aspect;
            Vector3 farCenterPoint = transform.position + distance * transform.forward;
            Vector3 up = upLen * transform.up;
            Vector3 right = rightLen * transform.right;
            points[0] = farCenterPoint - up - right;//left-bottom
            points[1] = farCenterPoint - up + right;//right-bottom
            points[2] = farCenterPoint + up - right;//left-up
            points[3] = farCenterPoint + up + right;//right-up
            return points;
        }

        public static Vector4[] GetFrustumPlane(Camera camera)
        {
            Vector4[] planes = new Vector4[6];
            Transform transform = camera.transform;
            Vector3 cameraPos = transform.position;
            
            Vector3[] points = GetCameraFarClipPlanePoint(camera);

            var forward = transform.forward;
            planes[0] = GetPlane(cameraPos, points[0], points[2]); //left
            planes[1] = GetPlane(cameraPos, points[3], points[1]); //right
            planes[2] = GetPlane(cameraPos, points[1], points[0]); //bottom
            planes[3] = GetPlane(cameraPos, points[2], points[3]); //up
            planes[4] = GetPlane(-forward, cameraPos + forward * camera.nearClipPlane); //near
            planes[5] = GetPlane(forward, cameraPos + forward * camera.farClipPlane);   //far
            
            return planes;
        }
    };
};