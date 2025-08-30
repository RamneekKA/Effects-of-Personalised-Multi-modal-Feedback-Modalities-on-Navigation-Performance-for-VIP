using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    public class PedestrianExample : MonoBehaviour
    {
        private const float _crossingTimer = 10;

        private float _time = 0;
        private void Update()
        {
            if (API.IsInitialized())
            {
                // Automatic change the crossing state.
                _time +=Time.deltaTime;

                if ( _time > _crossingTimer )
                {
                    _time = 0;
                    API.SetCrossingState("TrafficLightsIntersection1", !API.GetCrossingState("TrafficLightsIntersection1"));
                    API.SetCrossingState("TrafficLightsCrossing2", !API.GetCrossingState("TrafficLightsCrossing2"));
                }


                // Manual change the crossing state.
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    API.SetCrossingState("TrafficLightsIntersection1", !API.GetCrossingState("TrafficLightsIntersection1"));
                }

                if(Input.GetKeyDown(KeyCode.Alpha2))
                {
                    API.SetCrossingState("TrafficLightsCrossing2", !API.GetCrossingState("TrafficLightsCrossing2"));
                }
            }

            if(Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }
    }
}