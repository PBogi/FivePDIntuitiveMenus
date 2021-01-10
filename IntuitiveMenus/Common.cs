using Newtonsoft.Json.Linq;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core;


namespace IntuitiveMenus
{
    internal static class Common
    {
        internal static bool IsMenuOpen { get; set; } = false;
        internal static JObject Loadouts = new JObject();
        internal static JObject Vehicles = new JObject();
        internal static JObject Coordinates = new JObject();
        internal static bool IsAllowed = false;


        internal static void DisplayNotification(string text)
        {
            BeginTextCommandThefeedPost("STRING");
            AddTextComponentString(text);
            EndTextCommandThefeedPostTicker(true, true);
        }

        internal static void DrawMarker(int type, Vector3 coordinates, float scale)
        {
            /*float GroundZ = coordinates.Z;
            GetGroundZFor_3dCoord(coordinates.X, coordinates.Y, coordinates.Z, ref GroundZ, false);
            GroundZ += 0.005f;*/

            CitizenFX.Core.Native.API.DrawMarker(
                    type, // type
                    coordinates.X, // posX
                    coordinates.Y, // poxY
                    coordinates.Z, // posZ
                    0, // dirX
                    0, // dirY
                    0, // dirZ
                    0, // rotX
                    0, // rotY
                    0, // rotZ
                    scale, // scaleX
                    scale, // scaleY
                    1.0f, // scaleZ
                    100, // red
                    149, // green
                    237, // blue
                    50, // alpha
                    false, // bobUpAndDown
                    false, // faceCamera
                    2, // p19 .. Typically 2
                    false, // rotate
                    null, // textureDict
                    null, // textureName
                    false // drawOnEnts
                    );
        }
    }
}
